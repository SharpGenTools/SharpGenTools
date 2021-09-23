using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public IReadOnlyList<string> Rename(IReadOnlyList<CppField> cppFields) =>
            RenameMarshallableCore(cppFields, PostprocessFieldName);

        /// <summary>
        /// Renames the specified C++ parameters.
        /// </summary>
        /// <param name="cppParameters">The C++ parameters.</param>
        /// <returns>The C# names of these parameters.</returns>
        public IReadOnlyList<string> Rename(IReadOnlyList<CppParameter> cppParameters) =>
            RenameMarshallableCore(cppParameters, PostprocessParameterName);

        private delegate string PostprocessName<in T>(T element, string name, bool isFinal) where T : CppMarshallable;

        private static string PostprocessFieldName(CppField element, string name, bool isFinal) =>
            UnKeyword(FixDigitName(name, "Field"));

        private static string PostprocessParameterName(CppParameter element, string name, bool isFinal)
        {
            name = FixDigitName(name, "arg");

            if (!isFinal && !char.IsLower(name[0]))
                name = char.ToLower(name[0]) + name.Substring(1);

            return UnKeyword(name);
        }

        private IReadOnlyList<string> RenameMarshallableCore<T>(IReadOnlyList<T> cppItems,
                                                                PostprocessName<T> postprocessName)
            where T : CppMarshallable
        {
            if (cppItems == null) throw new ArgumentNullException(nameof(cppItems));
            if (postprocessName == null) throw new ArgumentNullException(nameof(postprocessName));

            var count = cppItems.Count;
            var names = new string[count][];
            var result = new string[count];
            HashSet<string> nameSet = new();

            for (var index = 0; index < count; index++)
            {
                var cppItem = cppItems[index];

                var originalName = cppItem.Name;
                var tag = cppItem.Rule;
                var name = RenameCore(originalName, tag, null, out var isFinal, out var isPreempted);

                if (isFinal)
                {
                    Debug.Assert(!isPreempted);

                    var finalName = postprocessName(cppItem, name, true);
                    nameSet.Add(finalName);
                    result[index] = finalName;
                }
                else
                {
                    List<string> variantList = new(2);

                    var namingFlags = tag.NamingFlags is { } flags ? flags : NamingFlags.Default;
                    var noPrematureBreak = (namingFlags & NamingFlags.NoPrematureBreak) != 0;

                    string PascalCaseIfNeeded(string name)
                    {
                        if (isPreempted && !noPrematureBreak)
                            return name;

                        return ConvertToPascalCase(name, namingFlags);
                    }

                    if (!isPreempted || noPrematureBreak)
                    {
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
                    }

                    if (variantList.Count == 0)
                        variantList.Add(name);

                    names[index] = variantList.Select(x => postprocessName(cppItem, PascalCaseIfNeeded(x), false))
                                              .ToArray();
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

                switch (nameList.Length)
                {
                    case < 1:
                        Debug.Fail($"Expected to have non-empty {nameof(nameList)}");
                        result[index] = "SHARPGEN_FAILURE";
                        continue;
                    case 1:
                        result[index] = DeduplicateName(nameList[0]);
                        continue;
                }

                var withoutDuplicates = nameList.Except(nameSet).ToArray();

                result[index] = DeduplicateName(
                    withoutDuplicates.Length switch
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