using System.Collections.Generic;
using System.Linq;
using SharpGen.CppModel;
using SharpGen.Model;

namespace SharpGen.UnitTests
{
    internal static class ModelTestExtensions
    {
        public static T FindFirst<T>(this CppElement element, string path) where T : CppElement =>
            element.Find<T>(path).FirstOrDefault();

        public static IEnumerable<T> Find<T>(this CppElement element, string path) where T : CppElement =>
            new CppElementFinder(element).Find<T>(path);

        public static IEnumerable<CsBase> EnumerateDescendants(this CsBase element, bool withAdditionalItems = true)
        {
            yield return element;

            IEnumerable<CsBase> items = element.Items;
            if (withAdditionalItems)
                items = items.Concat(element.AdditionalItems);

            foreach (var descendant in items.SelectMany(x => EnumerateDescendants(x, withAdditionalItems)))
                yield return descendant;
        }
    }
}
