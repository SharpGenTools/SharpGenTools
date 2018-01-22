using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGen.UnitTests
{
    static class ModelTestExtensions
    {
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
