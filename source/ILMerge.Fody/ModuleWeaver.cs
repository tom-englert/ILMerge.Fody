// #define LAUNCH_DEBUGGER

namespace ILMerge.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using FodyTools;

    using JetBrains.Annotations;

    using Mono.Cecil;

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

            var includesPattern = ReadConfigValue("IncludeAssemblies");
            var excludesPattern = ReadConfigValue("ExcludeAssemblies");
            var isDotNetCore = ModuleDefinition.IsTargetFrameworkDotNetCore();

            var references = isDotNetCore ? References.Split(';') : ReferenceCopyLocalPaths;

            var codeImporter = new CodeImporter(ModuleDefinition)
            {
                ModuleResolver = new LocalReferenceModuleResolver(this, references, includesPattern, excludesPattern)
            };

            codeImporter.ILMerge();

            var importedReferences = new HashSet<string>(codeImporter.ListImportedModules().Select(moduleDefinition => moduleDefinition.FileName), StringComparer.OrdinalIgnoreCase);

            // needs fody > 3.2.9 to work!!
            ReferenceCopyLocalPaths.RemoveAll(path => importedReferences.Contains(path));
        }

        private string ReadConfigValue(string name)
        {
            var customAttributes = ModuleDefinition.Assembly.CustomAttributes;
            var attribute = customAttributes.FirstOrDefault(item => item.AttributeType.FullName == $"ILMerge.{name}Attribute");
            if (attribute != null)
            {
                customAttributes.Remove(attribute);
                return attribute.ConstructorArguments.FirstOrDefault().Value as string;
            }

            return Config.Attribute(name)?.Value ?? Config.Descendants(name).SingleOrDefault()?.Value;
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
                return pattern != null ? new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase) : null;
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
