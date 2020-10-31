using System.Collections.Generic;
using System.Linq;
using SharpGen.CppModel;
using SharpGen.Model;

namespace SharpGen.UnitTests
{
    static class ModelTestExtensions
    {
        public static T FindFirst<T>(this CppElement element, string path)
            where T : CppElement => element.Find<T>(path).FirstOrDefault();

        public static IEnumerable<T> Find<T>(this CppElement element, string path)
            where T : CppElement
        {
            var mapper = new CppElementFinder(element);

            return mapper.Find<T>(path);
        }

        public static IEnumerable<CsBase> EnumerateDescendants(this CsBase element)
        {
            yield return element;
            foreach (var descendant in element.Items.SelectMany(EnumerateDescendants))
            {
                yield return descendant;
            }
        }
    }
}
