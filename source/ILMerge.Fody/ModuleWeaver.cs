#define LAUNCH_DEBUGGER

namespace ILMerge.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;

    using FodyTools;

    using JetBrains.Annotations;

    using Mono.Cecil;
    using Mono.Collections.Generic;

    public class ModuleWeaver : LoggingBaseModuleWeaver
    {
        [NotNull]
        public override IEnumerable<string> GetAssembliesForScanning() => Enumerable.Empty<string>();

        public override bool ShouldCleanReference => true;

        public override void Execute()
        {
#if DEBUG && LAUNCH_DEBUGGER && NETFRAMEWORK
            System.Diagnostics.Debugger.Launch();
#endif
            var module = ModuleDefinition;

            var includesPattern = ReadConfigValue("Include");
            var excludesPattern = ReadConfigValue("Exclude");

            var codeImporter = new CodeImporter(module)
            {
                ModuleResolver = new LocalReferenceModuleResolver(this, ReferenceCopyLocalPaths, includesPattern, excludesPattern)
            };

            var existingTypes = module.Types.ToArray();

            MergeAttributes(codeImporter, module);
            MergeAttributes(codeImporter, module.Assembly);

            foreach (var typeDefinition in existingTypes)
            {
                MergeAttributes(codeImporter, typeDefinition);
                MergeGenericParameters(codeImporter, typeDefinition);

                typeDefinition.BaseType = codeImporter.Import(typeDefinition.BaseType);

                foreach (var fieldDefinition in typeDefinition.Fields)
                {
                    MergeAttributes(codeImporter, fieldDefinition);
                    fieldDefinition.FieldType = codeImporter.Import(fieldDefinition.FieldType);
                }

                foreach (var eventDefinition in typeDefinition.Events)
                {
                    MergeAttributes(codeImporter, eventDefinition);
                    eventDefinition.EventType = codeImporter.Import(eventDefinition.EventType);
                }

                foreach (var propertyDefinition in typeDefinition.Properties)
                {
                    MergeAttributes(codeImporter, propertyDefinition);

                    propertyDefinition.PropertyType = codeImporter.Import(propertyDefinition.PropertyType);

                    if (!propertyDefinition.HasParameters)
                        continue;

                    foreach (var parameter in propertyDefinition.Parameters)
                    {
                        MergeAttributes(codeImporter, parameter);
                        parameter.ParameterType = codeImporter.Import(parameter.ParameterType);
                    }
                }

                foreach (var methodDefinition in typeDefinition.Methods)
                {
                    MergeAttributes(codeImporter, methodDefinition);
                    MergeGenericParameters(codeImporter, methodDefinition);

                    methodDefinition.ReturnType = codeImporter.Import(methodDefinition.ReturnType);

                    foreach (var parameter in methodDefinition.Parameters)
                    {
                        MergeAttributes(codeImporter, parameter);
                        parameter.ParameterType = codeImporter.Import(parameter.ParameterType);
                    }

                    var methodBody = methodDefinition.Body;
                    if (methodBody == null)
                        continue;

                    foreach (var variable in methodBody.Variables)
                    {
                        variable.VariableType = codeImporter.Import(variable.VariableType);
                    }

                    foreach (var instruction in methodBody.Instructions)
                    {
                        switch (instruction.Operand)
                        {
                            case MethodDefinition _:
                                break;

                            case GenericInstanceMethod genericInstanceMethod:
                                instruction.Operand = codeImporter.ImportGenericInstanceMethod(genericInstanceMethod);
                                break;

                            case MethodReference methodReference:
                                methodReference.DeclaringType = codeImporter.Import(methodReference.DeclaringType);
                                methodReference.ReturnType = codeImporter.Import(methodReference.ReturnType);
                                foreach (var parameter in methodReference.Parameters)
                                {
                                    parameter.ParameterType = codeImporter.Import(parameter.ParameterType);
                                }
                                break;

                            case TypeDefinition _:
                                break;

                            case TypeReference typeReference:
                                instruction.Operand = codeImporter.Import(typeReference);
                                break;

                            case FieldReference fieldReference:
                                fieldReference.FieldType = codeImporter.Import(fieldReference.FieldType);
                                break;
                        }
                    }
                }
            }

            var importedAssemblyNames = new HashSet<string>(codeImporter.ListImportedModules().Select(m => m.Assembly.FullName));

            module.AssemblyReferences.RemoveAll(ar => importedAssemblyNames.Contains(ar.FullName));
        }

        private static void MergeGenericParameters(CodeImporter codeImporter, IGenericParameterProvider provider)
        {
            if (provider?.HasGenericParameters != true)
                return;

            foreach (var parameter in provider.GenericParameters)
            {
                MergeTypes(codeImporter, parameter.Constraints);
            }
        }

        private static void MergeTypes(CodeImporter codeImporter, [NotNull] IList<TypeReference> types)
        {
            for (int i = 0; i < types.Count; i++)
            {
                types[i] = codeImporter.Import(types[i]);
            }
        }

        private static void MergeAttributes(CodeImporter codeImporter, ICustomAttributeProvider attributeProvider)
        {
            if (attributeProvider?.HasCustomAttributes != true)
                return;

            foreach (var attribute in attributeProvider.CustomAttributes)
            {
                attribute.Constructor.DeclaringType = codeImporter.Import(attribute.Constructor.DeclaringType);

                if (!attribute.HasConstructorArguments)
                    continue;

                for (var index = 0; index < attribute.ConstructorArguments.Count; index++)
                {
                    attribute.ConstructorArguments[index] = new CustomAttributeArgument(attribute.ConstructorArguments[index].Type, attribute.ConstructorArguments[index].Value);
                }
            }
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
