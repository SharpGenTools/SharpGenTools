using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SharpGen.Transform
{
    public sealed partial class NamingRulesManager
    {
        private readonly List<ShortNameMapper> _expandShortName = new();

        /// <summary>
        /// Adds the short name rule.
        /// </summary>
        /// <param name="regexShortName">Short name of the regex.</param>
        /// <param name="expandedName">Name of the expanded.</param>
        public void AddShortNameRule(string regexShortName, string expandedName)
        {
            _expandShortName.Add(new ShortNameMapper(regexShortName, expandedName));
            _expandShortName.Sort();
        }

        private class ShortNameMapper : IComparable<ShortNameMapper>, IComparable
        {
            public ShortNameMapper(string regex, string replace)
            {
                Regex = new Regex("^" + regex);
                Replace = replace;
                HasRegexReplace = replace.Contains("$");
            }

            public readonly Regex Regex;

            public readonly string Replace;

            public readonly bool HasRegexReplace;

            public int CompareTo(ShortNameMapper other)
            {
                return -Regex.ToString().Length.CompareTo((int) other.Regex.ToString().Length);
            }

            public int CompareTo(object obj)
            {
                return obj is ShortNameMapper mapper ? CompareTo(mapper) : throw new InvalidOperationException();
            }
        }
    }
}