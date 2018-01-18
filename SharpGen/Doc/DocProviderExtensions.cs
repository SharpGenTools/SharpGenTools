using SharpGen.CppModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Doc
{
    public static class DocProviderExtensions
    {
        public static CppModule ApplyDocumentation(this IDocProvider docProvider, CppModule module)
        {
            docProvider.Begin();

            foreach (CppInclude cppInclude in module.Includes)
            {
                foreach (CppEnum cppEnum in cppInclude.Enums)
                {
                    DocItem docItem = docProvider.FindDocumentation(cppEnum.Name);
                    cppEnum.Id = docItem.Id;
                    cppEnum.Description = docItem.Description;
                    cppEnum.Remarks = docItem.Remarks;

                    if (cppEnum.IsEmpty)
                        continue;

                    var count = Math.Min(cppEnum.Items.Count, docItem.Items.Count);
                    var i = 0;
                    foreach (CppEnumItem cppEnumItem in cppEnum.EnumItems)
                    {
                        cppEnumItem.Id = docItem.Id;

                        // Try to find the matching item
                        var foundMatch = false;
                        foreach (var subItem in docItem.Items)
                        {
                            if (Utilities.ContainsCppIdentifier(subItem.Term, cppEnumItem.Name))
                            {
                                cppEnumItem.Description = subItem.Description;
                                foundMatch = true;
                                break;
                            }
                        }
                        if (!foundMatch && i < count)
                            cppEnumItem.Description = docItem.Items[i].Description;
                        i++;
                    }
                }

                foreach (CppStruct cppStruct in cppInclude.Structs)
                {
                    DocItem docItem = docProvider.FindDocumentation(cppStruct.Name);
                    cppStruct.Id = docItem.Id;
                    cppStruct.Description = docItem.Description;
                    cppStruct.Remarks = docItem.Remarks;

                    if (cppStruct.IsEmpty)
                        continue;

                    if (cppStruct.Items.Count != docItem.Items.Count)
                    {
                        //Logger.Warning("Invalid number of fields in documentation for Struct {0}", cppStruct.Name);
                    }
                    var count = Math.Min(cppStruct.Items.Count, docItem.Items.Count);
                    var i = 0;
                    foreach (CppField cppField in cppStruct.Fields)
                    {
                        cppField.Id = docItem.Id;

                        // Try to find the matching item
                        var foundMatch = false;
                        foreach (var subItem in docItem.Items)
                        {
                            if (Utilities.ContainsCppIdentifier(subItem.Term, cppField.Name))
                            {
                                cppField.Description = subItem.Description;
                                foundMatch = true;
                                break;
                            }
                        }
                        if (!foundMatch && i < count)
                            cppField.Description = docItem.Items[i].Description;
                        i++;
                    }
                }

                foreach (CppInterface cppInterface in cppInclude.Interfaces)
                {
                    DocItem docItem = docProvider.FindDocumentation(cppInterface.Name);
                    cppInterface.Id = docItem.Id;
                    cppInterface.Description = docItem.Description;
                    cppInterface.Remarks = docItem.Remarks;

                    if (cppInterface.IsEmpty)
                        continue;

                    foreach (CppMethod cppMethod in cppInterface.Methods)
                    {
                        var methodName = cppInterface.Name + "::" + cppMethod.Name;
                        DocItem methodDocItem = docProvider.FindDocumentation(methodName);
                        cppMethod.Id = methodDocItem.Id;
                        cppMethod.Description = methodDocItem.Description;
                        cppMethod.Remarks = methodDocItem.Remarks;
                        cppMethod.ReturnValue.Description = methodDocItem.Return;

                        if (cppMethod.IsEmpty)
                            continue;

                        var count = Math.Min(cppMethod.Items.Count, methodDocItem.Items.Count);
                        var i = 0;
                        foreach (CppParameter cppParameter in cppMethod.Parameters)
                        {
                            cppParameter.Id = methodDocItem.Id;

                            // Try to find the matching item
                            var foundMatch = false;
                            foreach (var subItem in methodDocItem.Items)
                            {
                                if (Utilities.ContainsCppIdentifier(subItem.Term, cppParameter.Name))
                                {
                                    cppParameter.Description = subItem.Description;
                                    foundMatch = true;
                                    break;
                                }
                            }
                            if (!foundMatch && i < count)
                                cppParameter.Description = methodDocItem.Items[i].Description;
                            i++;
                        }
                    }
                }

                foreach (CppFunction cppFunction in cppInclude.Functions)
                {
                    DocItem docItem = docProvider.FindDocumentation(cppFunction.Name);
                    cppFunction.Id = docItem.Id;
                    cppFunction.Description = docItem.Description;
                    cppFunction.Remarks = docItem.Remarks;
                    cppFunction.ReturnValue.Description = docItem.Return;

                    if (cppFunction.IsEmpty)
                        continue;

                    var count = Math.Min(cppFunction.Items.Count, docItem.Items.Count);
                    var i = 0;
                    foreach (CppParameter cppParameter in cppFunction.Parameters)
                    {
                        cppParameter.Id = docItem.Id;

                        // Try to find the matching item
                        var foundMatch = false;
                        foreach (var subItem in docItem.Items)
                        {
                            if (Utilities.ContainsCppIdentifier(subItem.Term, cppParameter.Name))
                            {
                                cppParameter.Description = subItem.Description;
                                foundMatch = true;
                                break;
                            }
                        }
                        if (!foundMatch && i < count)
                            cppParameter.Description = docItem.Items[i].Description;
                        i++;
                    }
                }
            }
            docProvider.End();

            return module;
        }
    }
}
