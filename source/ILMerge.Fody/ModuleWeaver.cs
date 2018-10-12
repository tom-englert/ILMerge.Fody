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
#if DEBUG && LAUNCH_DEBUGGER // && NETFRAMEWORK
            System.Diagnostics.Debugger.Launch();
#endif

            var includesPattern = ReadConfigValue("Include");
            var excludesPattern = ReadConfigValue("Exclude");

            // ReSharper disable once JoinDeclarationAndInitializer
            IList<string> references;

            #if NETSTANDARD
            references = References.Split(';');
            #else
            references = ReferenceCopyLocalPaths;
            #endif


            var codeImporter = new CodeImporter(ModuleDefinition)
            {
                ImportPropertiesAndEvents = false,
                ModuleResolver = new LocalReferenceModuleResolver(this, references, includesPattern, excludesPattern)
            };

            codeImporter.ILMerge();
        }

        private string ReadConfigValue(string name)
        {
            var customAttributes = ModuleDefinition.Assembly.CustomAttributes;
            var attribute = customAttributes.FirstOrDefault(item => item.AttributeType.FullName == $"ILMerge.{name}AssembliesAttribute");
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
            private readonly HashSet<string> _referenceCopyLocalPaths;
            [NotNull]
            private readonly HashSet<string> _ignoredAssemblyNames = new HashSet<string>();

            public LocalReferenceModuleResolver([NotNull] ILogger logger, [NotNull] IEnumerable<string> referenceCopyLocalPaths, string includesPattern, string excludesPattern)
            {
                _logger = logger;
                _includes = BuildRegex(includesPattern);
                _excludes = BuildRegex(excludesPattern);

                _referenceCopyLocalPaths = new HashSet<string>(referenceCopyLocalPaths, StringComparer.OrdinalIgnoreCase);
            }

            public ModuleDefinition Resolve(TypeReference typeReference, string assemblyName)
            {
                if (_ignoredAssemblyNames.Contains(assemblyName))
                    return null;

                var module = typeReference.Resolve().Module;
                var moduleAssemblyName = module.Assembly.Name.Name;

                if (!_referenceCopyLocalPaths.Contains(module.FileName))
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
}
