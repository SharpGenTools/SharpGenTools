using System;
using System.Collections.Generic;
using System.Linq;
using SharpGen.CppModel;

namespace SharpGen.Transform
{
    public sealed partial class NamingRulesManager
    {
        private readonly List<HungarianNotationPrefix> _hungarianNotation = new()
        {
            new HungarianNotationPrefix("pp", HungarianOutPrefixSelector, "Out", "ref", "pointer", "array"),
            new HungarianNotationPrefix("p", HungarianRefPrefixSelector, "Ref", "Pointer", "Array", "out"),
            new HungarianNotationPrefix("lp", HungarianRefPrefixSelector, "Ref", "Pointer", "Array", "out"),
            new HungarianNotationPrefix("u", HungarianIntegerPrefixSelector, string.Empty),
            new HungarianNotationPrefix("ul", HungarianIntegerPrefixSelector, string.Empty),
            new HungarianNotationPrefix("dw", HungarianIntegerPrefixSelector, string.Empty),
            new HungarianNotationPrefix("fn", HungarianFunctionPrefixSelector, "Function", "delegate"),
            new HungarianNotationPrefix("pfn", HungarianFunctionPrefixSelector, "Function", "delegate"),
            new HungarianNotationPrefix("f", HungarianIntegerPrefixSelector, "Flag", "enable"),
            new HungarianNotationPrefix("cb", HungarianIntegerPrefixSelector, "ByteCount", "size", "length"),
            new HungarianNotationPrefix("h", HungarianFunctionPrefixSelector, "Handle"),
            new HungarianNotationPrefix("str", HungarianStringPrefixSelector, "String"),
            new HungarianNotationPrefix("wsz", HungarianStringPrefixSelector, "String"),
        };

        private static int HungarianOutPrefixSelector(CppMarshallable x) => x is CppParameter {HasPointer: true}
            ? 0
            : -1;

        private static int HungarianRefPrefixSelector(CppMarshallable x) => x switch
        {
            CppParameter {HasPointer: true} => 0,
            CppField {HasPointer: true, IsArray: false, ArrayDimension: null or {Length: 0}} => 1,
            CppField {IsArray: true} => 2,
            _ => -1
        };

        private static int HungarianIntegerPrefixSelector(CppMarshallable x) =>
            x is {HasPointer: false, IsArray: false, ArrayDimension: null or {Length: 0}}
                ? 0
                : -1;

        private static int HungarianFunctionPrefixSelector(CppMarshallable x) =>
            x is {IsArray: false, ArrayDimension: null or {Length: 0}}
                ? 0
                : -1;

        private static int HungarianStringPrefixSelector(CppMarshallable x) => x is {HasPointer: true}
            ? 0
            : -1;

        private sealed class HungarianNotationPrefix
        {
            public HungarianNotationPrefix(string prefix, Func<CppMarshallable, int> selector,
                                           params string[] suffixVariants)
            {
                Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
                Selector = selector ?? throw new ArgumentNullException(nameof(selector));
                SuffixVariants = suffixVariants;
                LowerSuffixVariants = suffixVariants.Select(x => x.ToLowerInvariant()).ToArray();
            }

            private string Prefix { get; }
            private Func<CppMarshallable, int> Selector { get; }
            private string[] SuffixVariants { get; }
            private string[] LowerSuffixVariants { get; }

            public bool Apply(CppMarshallable marshallable, string name, string originalName, string originalNameLower,
                              out string[] variants)
            {
                variants = null;
                if (originalName.Length <= Prefix.Length + 1)
                    return false;

                if (!originalName.StartsWith(Prefix))
                    return false;

                if (!char.IsUpper(originalName[Prefix.Length]))
                    return false;

                var index = Selector(marshallable);
                if (index < 0 || index >= SuffixVariants.Length)
                    return false;

                var content = name.Substring(Prefix.Length);
                var contentLower = content.ToLowerInvariant();
                var suffixedVariant = content + SuffixVariants[index];

                bool SuffixPredicate(string x) => contentLower.Contains(x) || originalNameLower.Contains(x);

                variants = LowerSuffixVariants.Any(SuffixPredicate)
                               ? new[] {content, suffixedVariant}
                               : new[] {suffixedVariant, content};

                return true;
            }
        }
    }
}