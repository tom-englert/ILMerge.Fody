// #define LAUNCH_DEBUGGER

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Fody;
using FodyTools;
using JetBrains.Annotations;
using Mono.Cecil;

namespace ILMerge.Fody
{
    public class ModuleWeaver : AbstractModuleWeaver
    {
        [NotNull]
        public override IEnumerable<string> GetAssembliesForScanning() => Enumerable.Empty<string>();

        public override bool ShouldCleanReference => true;

        public override void Execute()
        {
#if DEBUG && LAUNCH_DEBUGGER
            System.Diagnostics.Debugger.Launch();
#endif

            var includesPattern = ReadConfigValue("IncludeAssemblies", string.Empty);
            var excludesPattern = ReadConfigValue("ExcludeAssemblies", string.Empty);
            var hideImportedTypes = ReadConfigValue("HideImportedTypes", true);

            var isDotNetCore = ModuleDefinition.IsTargetFrameworkDotNetCore();

            var references = isDotNetCore ? References.Split(';') : ReferenceCopyLocalPaths;

            var codeImporter = new CodeImporter(ModuleDefinition)
            {
                ModuleResolver = new LocalReferenceModuleResolver(this, references, includesPattern, excludesPattern),
                HideImportedTypes = hideImportedTypes
            };

            codeImporter.ILMerge();

            var importedReferences = new HashSet<string>(codeImporter.ListImportedModules().Select(moduleDefinition => Path.GetFileNameWithoutExtension(moduleDefinition.FileName)), StringComparer.OrdinalIgnoreCase);

            ReferenceCopyLocalPaths.RemoveAll(path => importedReferences.Contains(Path.GetFileNameWithoutExtension(path)));
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
                return XmlConvert.ToBoolean((ReadConfigValue(name, defaultValue.ToString(CultureInfo.InvariantCulture)) ?? defaultValue.ToString()).ToLowerInvariant());
            }
            catch (Exception ex)
            {
                throw new WeavingException($"Error parsing the configuration value '{name}'");
            }
        }

        private class LocalReferenceModuleResolver : IModuleResolver
        {
            [NotNull]
            private readonly ILogger _logger;
            [CanBeNull]
            private readonly Regex _includes;
            [CanBeNull]
            private readonly Regex _excludes;
            [NotNull]
            private readonly HashSet<string> _referencePaths;
            [NotNull]
            private readonly HashSet<string> _ignoredAssemblyNames = new HashSet<string>();

            public LocalReferenceModuleResolver([NotNull] ILogger logger, [NotNull] IEnumerable<string> referencePaths, string includesPattern, string excludesPattern)
            {
                _logger = logger;
                _includes = BuildRegex(includesPattern);
                _excludes = BuildRegex(excludesPattern);

                _referencePaths = new HashSet<string>(referencePaths, StringComparer.OrdinalIgnoreCase);
            }

            public ModuleDefinition Resolve(TypeReference typeReference, string assemblyName)
            {
                if (_ignoredAssemblyNames.Contains(assemblyName))
                    return null;

                var module = typeReference.Resolve().Module;
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

                _ignoredAssemblyNames.Add(assemblyName);
                return null;
            }

            private static Regex BuildRegex(string pattern)
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
        }
    }

    static class ExtensionMethods
    {
        public static string GetTargetFramework([NotNull] this ModuleDefinition moduleDefinition)
        {
            return moduleDefinition.Assembly
                .CustomAttributes
                .Where(attr => attr.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute")
                .Select(attr => attr.ConstructorArguments.Select(arg => arg.Value as string).FirstOrDefault())
                .FirstOrDefault();
        }

        public static bool IsTargetFrameworkDotNetCore([NotNull] this ModuleDefinition moduleDefinition)
        {
            var targetFramework = moduleDefinition.GetTargetFramework();

            return targetFramework?.StartsWith(".NETCoreApp", StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }
}
