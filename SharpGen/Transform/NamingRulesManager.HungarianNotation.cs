using System;
using System.Collections.Generic;
using System.Linq;
using SharpGen.CppModel;

namespace SharpGen.Transform;

public sealed partial class NamingRulesManager
{
    private readonly List<HungarianNotationPrefix> _hungarianNotation = new()
    {
        new HungarianNotationPrefix("pp", HungarianOutPrefixSelector, "Out", "Ref", "Pointer", "Array"),
        new HungarianNotationPrefix("p", HungarianRefPrefixSelector, "Ref", "Pointer", "Array", "Out"),
        new HungarianNotationPrefix("lp", HungarianRefPrefixSelector, "Ref", "Pointer", "Array", "Out"),
        new HungarianNotationPrefix("pv", HungarianRefPrefixSelector, "Ref", "Pointer", "Array", "Out"),
        new HungarianNotationPrefix("b", HungarianIntegerPrefixSelector),
        new HungarianNotationPrefix("u", HungarianIntegerPrefixSelector),
        new HungarianNotationPrefix("n", HungarianIntegerPrefixSelector, string.Empty, "Count", "Int"),
        new HungarianNotationPrefix("i", HungarianIntegerPrefixSelector, string.Empty, "Index", "Int"),
        new HungarianNotationPrefix("ul", HungarianIntegerPrefixSelector),
        new HungarianNotationPrefix("us", HungarianIntegerPrefixSelector),
        new HungarianNotationPrefix("l", HungarianIntegerPrefixSelector),
        new HungarianNotationPrefix("w", HungarianIntegerPrefixSelector),
        new HungarianNotationPrefix("dw", HungarianIntegerPrefixSelector),
        new HungarianNotationPrefix("qw", HungarianIntegerPrefixSelector),
        new HungarianNotationPrefix("fn", HungarianFunctionPrefixSelector, "Function", "Delegate"),
        new HungarianNotationPrefix("pfn", HungarianFunctionPrefixSelector, "Function", "Delegate"),
        new HungarianNotationPrefix("lpfn", HungarianFunctionPrefixSelector, "Function", "Delegate"),
        new HungarianNotationPrefix("f", HungarianIntegerPrefixSelector, "Flag", "Enable", "Float"),
        new HungarianNotationPrefix("cb", HungarianIntegerPrefixSelector, "ByteCount", "Size", "Length"),
        new HungarianNotationPrefix("h", HungarianFunctionPrefixSelector, "Handle"),
        new HungarianNotationPrefix("str", HungarianStringPrefixSelector, "String", "Text"),
        new HungarianNotationPrefix("bstr", HungarianStringPrefixSelector, "String", "Text"),
        new HungarianNotationPrefix("pbstr", HungarianStringPrefixSelector, "String", "Text"),
        new HungarianNotationPrefix("wsz", HungarianStringPrefixSelector, "String", "Text"),
        new HungarianNotationPrefix("pstr", HungarianStringPrefixSelector, "String", "Text"),
        new HungarianNotationPrefix("pwsz", HungarianStringPrefixSelector, "String", "Text"),
        new HungarianNotationPrefix("wcs", HungarianStringPrefixSelector, "String", "Text"),
        new HungarianNotationPrefix("pwcs", HungarianStringPrefixSelector, "String", "Text"),
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
            if (index < 0)
                return false;

            var content = name.Substring(Prefix.Length);
            var contentLower = content.ToLowerInvariant();

            string SuffixWith(string suffix) => content + suffix;
            bool SuffixPredicate(string x) => !contentLower.Contains(x) && !originalNameLower.Contains(x);

            IEnumerable<string> preferredSuffixedVariants, secondarySuffixedVariants;
            switch (SuffixVariants.Length)
            {
                case > 0 when index >= SuffixVariants.Length:
                    return false;
                case > 0:
                    preferredSuffixedVariants = new[] { SuffixVariants[index] }
                                               .Where(SuffixPredicate).Select(SuffixWith);
                    secondarySuffixedVariants = SuffixVariants.Take(index)
                                                              .Concat(SuffixVariants.Skip(index + 1))
                                                              .Where(SuffixPredicate)
                                                              .Select(SuffixWith);
                    break;
                default:
                    preferredSuffixedVariants = secondarySuffixedVariants = Enumerable.Empty<string>();
                    break;
            }

            variants = new[] { content }.Concat(preferredSuffixedVariants).Concat(secondarySuffixedVariants)
                                        .ToArray();

            return true;
        }
    }
}