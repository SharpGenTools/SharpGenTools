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

        public static CsBase[] EnumerateDescendants(this CsBase element, bool withAdditionalItems = true) =>
            ModelUtilities.EnumerateDescendants(element, withAdditionalItems).ToArray();

        public static T[] EnumerateDescendants<T>(this CsBase element, bool withAdditionalItems = true)
            where T : CsBase => ModelUtilities.EnumerateDescendants<T>(element, withAdditionalItems).ToArray();
    }
}
