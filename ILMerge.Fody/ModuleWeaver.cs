// #define LAUNCH_DEBUGGER

namespace ILMerge.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml;

    using FodyTools;

    using global::Fody;

    using Mono.Cecil;

    public class ModuleWeaver : AbstractModuleWeaver
    {
        public override IEnumerable<string> GetAssembliesForScanning() => Enumerable.Empty<string>();

        public override bool ShouldCleanReference => true;

        public override void Execute()
        {
#if DEBUG && LAUNCH_DEBUGGER
            System.Diagnostics.Debugger.Launch();
#endif

            var includeAssemblies = BuildRegex(ReadConfigValue("IncludeAssemblies", string.Empty));
            var excludeAssemblies = BuildRegex(ReadConfigValue("ExcludeAssemblies", string.Empty));
            var includeResources = BuildRegex(ReadConfigValue("IncludeResources", string.Empty));
            var excludeResources = BuildRegex(ReadConfigValue("ExcludeResources", string.Empty));
            var hideImportedTypes = ReadConfigValue("HideImportedTypes", true);
            var namespacePrefix = ReadConfigValue("NamespacePrefix", string.Empty);
            var fullImport = ReadConfigValue("FullImport", false);

            var isDotNetCore = ModuleDefinition.IsTargetFrameworkDotNetCore();

            var references = isDotNetCore ? (IList<string>)References.Split(';') : ReferenceCopyLocalPaths;

            var codeImporter = new CodeImporter(ModuleDefinition)
            {
                ModuleResolver = new LocalReferenceModuleResolver(this, references, includeAssemblies, excludeAssemblies),
                HideImportedTypes = hideImportedTypes,
                NamespaceDecorator = name => namespacePrefix + name
            };

            codeImporter.ILMerge();

            var importedModules = codeImporter.ListImportedModules();

            if (fullImport)
            {
                importedModules = ImportRemainingTypes(importedModules, codeImporter);
            }

            ImportResources(ModuleDefinition, importedModules, includeResources, excludeResources, this);

            var importedReferences = new HashSet<string>(importedModules.Select(moduleDefinition => Path.GetFileNameWithoutExtension(moduleDefinition.FileName)), StringComparer.OrdinalIgnoreCase);

            ReferenceCopyLocalPaths.RemoveAll(path => importedReferences.Contains(Path.GetFileNameWithoutExtension(path)));
        }

        private static ICollection<ModuleDefinition> ImportRemainingTypes(ICollection<ModuleDefinition> importedModules, CodeImporter codeImporter)
        {
            var updatedModules = new HashSet<ModuleDefinition>();

            while (true)
            {
                var modulesToUpdate = importedModules.Except(updatedModules).ToList();
                if (!modulesToUpdate.Any())
                    break;

                foreach (var type in modulesToUpdate.SelectMany(module => module.GetTypes().Where(t => t.Name != "<Module>")))
                {
                    codeImporter.ImportType(type, null);
                }

                updatedModules = new HashSet<ModuleDefinition>(updatedModules.Concat(modulesToUpdate));
                importedModules = codeImporter.ListImportedModules();
            }

            return importedModules;
        }

        private static void ImportResources(ModuleDefinition targetModule, IEnumerable<ModuleDefinition> importedModules, Regex? includeResources, Regex? excludeResources, ILogger logger)
        {
            foreach (var resource in importedModules.SelectMany(module => module.Resources.OfType<EmbeddedResource>()))
            {
                var resourceName = resource.Name;

                if (excludeResources?.Match(resourceName).Success == true)
                {
                    logger.LogInfo($"Exclude resource {resourceName} because its in the exclude list.");
                }
                else if (includeResources?.Match(resourceName).Success == false)
                {
                    logger.LogInfo($"Exclude resource {resourceName} because its not in the include list.");
                }
                else
                {
                    logger.LogInfo($"Merge resource {resourceName}.");

                    targetModule.Resources.Add(resource);
                }
            }
        }

        private string ReadConfigValue(string name, string defaultValue)
        {
            var customAttributes = ModuleDefinition.Assembly.CustomAttributes;
            var attribute = customAttributes.FirstOrDefault(item => item.AttributeType.FullName == $"ILMerge.{name}Attribute");
            if (attribute != null)
            {
                customAttributes.Remove(attribute);
                return attribute.ConstructorArguments.FirstOrDefault().Value.ToString();
            }

            return Config.Attribute(name)?.Value ?? Config.Descendants(name).SingleOrDefault()?.Value ?? defaultValue;
        }

        private bool ReadConfigValue(string name, bool defaultValue)
        {
            try
            {
                return XmlConvert.ToBoolean(ReadConfigValue(name, defaultValue.ToString(CultureInfo.InvariantCulture)).ToLowerInvariant());
            }
            catch
            {
                throw new WeavingException($"Error parsing the configuration value '{name}'");
            }
        }

        private static Regex? BuildRegex(string pattern)
        {
            try
            {
                return string.IsNullOrEmpty(pattern) ? null : new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                throw new WeavingException($"Error parsing the regular expression '{pattern}': {ex.Message}");
            }
        }

        private class LocalReferenceModuleResolver : IModuleResolver
        {
            private readonly ILogger _logger;
            private readonly Regex? _includes;
            private readonly Regex? _excludes;
            private readonly HashSet<string> _referencePaths;
            private readonly HashSet<string> _ignoredAssemblyNames = new HashSet<string>();

            public LocalReferenceModuleResolver(ILogger logger, IEnumerable<string> referencePaths, Regex? includes, Regex? excludes)
            {
                _logger = logger;
                _includes = includes;
                _excludes = excludes;

                _referencePaths = new HashSet<string>(referencePaths, StringComparer.OrdinalIgnoreCase);
            }

            public ModuleDefinition? Resolve(TypeReference typeReference, string assemblyName)
            {
                if (_ignoredAssemblyNames.Contains(assemblyName))
                    return null;

                var module = typeReference.Resolve()?.Module;
                if (module != null)
                {
                    var moduleAssemblyName = module.Assembly.Name.Name;

                    if (!_referencePaths.Contains(module.FileName))
                    {
                        _logger.LogInfo($"Exclude assembly {assemblyName} because its not in the local references list.");
                    }
                    else
                    {
                        if (_excludes?.Match(moduleAssemblyName).Success == true)
                        {
                            _logger.LogInfo($"Exclude assembly {assemblyName} because its in the exclude list.");
                        }
                        else if (_includes?.Match(moduleAssemblyName).Success == false)
                        {
                            _logger.LogInfo($"Exclude assembly {assemblyName} because its not in the include list.");
                        }
                        else
                        {
                            _logger.LogInfo($"Merge types from assembly {assemblyName}.");
                            return module;
                        }
                    }
                }

                _ignoredAssemblyNames.Add(assemblyName);
                return null;
            }
        }
    }

    static class ExtensionMethods
    {
        public static string? GetTargetFramework(this ModuleDefinition moduleDefinition)
        {
            return moduleDefinition.Assembly
                .CustomAttributes
                .Where(attr => attr.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute")
                .Select(attr => attr.ConstructorArguments.Select(arg => arg.Value as string).FirstOrDefault())
                .FirstOrDefault();
        }

        public static bool IsTargetFrameworkDotNetCore(this ModuleDefinition moduleDefinition)
        {
            var targetFramework = moduleDefinition.GetTargetFramework();

            return targetFramework?.StartsWith(".NETCoreApp", StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }
}
