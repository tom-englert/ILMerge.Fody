namespace ILMerge.Fody
{
    using System.Collections.Generic;
    using System.Linq;

    using FodyTools;

    using Mono.Cecil;

    public class ModuleWeaver : LoggingBaseModuleWeaver
    {
        public override void Execute()
        {
            var module = ModuleDefinition;

            var codeImporter = new CodeImporter(module) { ModuleResolver = new LocalReferenceModuleResolver() };

            var types = module.Types.ToArray();

            MergeAttributes(codeImporter, module);
            MergeAttributes(codeImporter, module.Assembly);

            foreach (var type in types)
            {
                MergeAttributes(codeImporter, type);

                foreach (var fieldDefinition in type.Fields)
                {
                    fieldDefinition.FieldType = codeImporter.Import(fieldDefinition.FieldType);
                    MergeAttributes(codeImporter, fieldDefinition);
                }

                foreach (var eventDefinition in type.Events)
                {
                    eventDefinition.EventType = codeImporter.Import(eventDefinition.EventType);
                    MergeAttributes(codeImporter, eventDefinition);
                }

                foreach (var propertyDefinition in type.Properties)
                {
                    propertyDefinition.PropertyType = codeImporter.Import(propertyDefinition.PropertyType);
                    MergeAttributes(codeImporter, propertyDefinition);

                    if (!propertyDefinition.HasParameters)
                        continue;

                    foreach (var parameter in propertyDefinition.Parameters)
                    {
                        parameter.ParameterType = codeImporter.Import(parameter.ParameterType);
                    }
                }

                foreach (var methodDefinition in type.Methods)
                {
                    methodDefinition.ReturnType = codeImporter.Import(methodDefinition.ReturnType);
                    MergeAttributes(codeImporter, methodDefinition);

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
                                for (var index = 0; index < genericInstanceMethod.GenericArguments.Count; index++)
                                {
                                    genericInstanceMethod.GenericArguments[index] = codeImporter.Import(genericInstanceMethod.GenericArguments[index]);
                                }

                                genericInstanceMethod.ReturnType = codeImporter.Import(genericInstanceMethod.ReturnType);
                                break;

                            case MethodReference sourceMethodReference:
                                sourceMethodReference.DeclaringType = codeImporter.Import(sourceMethodReference.DeclaringType);
                                sourceMethodReference.ReturnType = codeImporter.Import(sourceMethodReference.ReturnType);
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

        private void MergeAttributes(CodeImporter codeImporter, ICustomAttributeProvider attributeProvider)
        {
            if (!attributeProvider.HasCustomAttributes)
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

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield break;
        }
    }
}
