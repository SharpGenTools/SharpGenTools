using System;
using System.Collections.Generic;
using System.Linq;
using SharpGen.Config;
using SharpGen.CppModel;

namespace SharpGen.Transform
{
    public sealed partial class NamingRulesManager
    {
        /// <summary>
        /// Renames the specified C++ fields.
        /// </summary>
        /// <param name="cppFields">The C++ fields.</param>
        /// <returns>The C# names of these fields.</returns>
        public IReadOnlyList<string> Rename(CppField[] cppFields) =>
            RenameMarshallableCore(cppFields, PostprocessFieldName);

        /// <summary>
        /// Renames the specified C++ parameters.
        /// </summary>
        /// <param name="cppParameters">The C++ parameters.</param>
        /// <returns>The C# names of these parameters.</returns>
        public IReadOnlyList<string> Rename(CppParameter[] cppParameters) =>
            RenameMarshallableCore(cppParameters, PostprocessParameterName);

        private string PostprocessFieldName(CppMarshallable element, string name, bool isFinal)
        {
            if (!isFinal)
            {
                var namingFlags = element.Rule.NamingFlags is { } flags ? flags : NamingFlags.Default;
                name = ConvertToPascalCase(name, namingFlags);
            }

            if (char.IsDigit(name[0]))
                name = "Field" + name;

            return UnKeyword(name);
        }

        private string PostprocessParameterName(CppMarshallable element, string name, bool isFinal)
        {
            if (!isFinal)
            {
                var namingFlags = element.Rule.NamingFlags is { } flags ? flags : NamingFlags.Default;
                name = ConvertToPascalCase(name, namingFlags);
            }

            if (char.IsDigit(name[0]))
                name = "arg" + name;

            if (!isFinal && !char.IsLower(name[0]))
                name = char.ToLower(name[0]) + name.Substring(1);

            return UnKeyword(name);
        }

        private IReadOnlyList<string> RenameMarshallableCore(IReadOnlyList<CppMarshallable> cppItems,
                                                             Func<CppMarshallable, string, bool, string>
                                                                 postprocessName)
        {
            var count = cppItems.Count;
            var names = new List<string>[count];
            var result = new string[count];
            HashSet<string> nameSet = new();

            for (var index = 0; index < count; index++)
            {
                var cppItem = cppItems[index];

                var originalName = cppItem.Name;
                var tag = cppItem.Rule;
                var name = RenameCore(originalName, tag, null, out var isFinal);

                if (isFinal)
                {
                    var finalName = postprocessName(cppItem, name, true);
                    nameSet.Add(finalName);
                    result[index] = finalName;
                }
                else
                {
                    List<string> variantList = new(2);

                    var namingFlags = tag.NamingFlags is { } flags ? flags : NamingFlags.Default;

                    if ((namingFlags & NamingFlags.NoHungarianNotationHandler) == 0)
                    {
                        var originalNameLower = originalName.ToLowerInvariant();
                        foreach (var prefix in _hungarianNotation)
                            if (prefix.Apply(cppItem, name, originalName, originalNameLower, out var variants))
                            {
                                variantList.AddRange(variants);
                                break;
                            }
                    }

                    if (variantList.Count == 0)
                        variantList.Add(name);

                    names[index] = variantList.Select(x => postprocessName(cppItem, x, false)).ToList();
                }
            }

            Dictionary<string, uint> duplicateCount = new();

            string DeduplicateName(string name)
            {
                if (nameSet.Add(name))
                    return name;

                string newName;
                do
                {
                    if (!duplicateCount.TryGetValue(name, out var nameCount))
                        nameCount = 0;

                    duplicateCount[name] = ++nameCount;
                    newName = name + nameCount;
                } while (!nameSet.Add(newName));

                return newName;
            }

            for (var index = 0; index < count; index++)
            {
                if (result[index] != null)
                    continue;

                var nameList = names[index];

                switch (nameList.Count)
                {
                    case < 1:
                        result[index] = "SHARPGEN_FAILURE";
                        continue;
                    case 1:
                        result[index] = DeduplicateName(nameList[0]);
                        continue;
                }

                var withoutDuplicates = nameList.Except(nameSet).ToList();

                result[index] = DeduplicateName(
                    withoutDuplicates.Count switch
                    {
                        >= 1 => withoutDuplicates[0],
                        < 1 => nameList[0]
                    }
                );
            }

            return result;
        }
    }
}