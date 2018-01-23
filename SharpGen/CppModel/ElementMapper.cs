using SharpGen.CppModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpGen.CppModel
{
    public class ElementMapper
    {
        public ElementMapper(CppElement root)
        {
            Root = root;
        }

        public CppElement Root { get; }

        /// <summary>
        ///   Gets the context find list.
        /// </summary>
        /// <value>The context find list.</value>
        private List<string> CurrentContexts { get; } = new List<string>();


        /// <summary>
        ///   Adds a context to the finder.
        /// </summary>
        /// <param name = "contextName">Name of the context.</param>
        public void AddContextFind(string contextName)
        {
            CurrentContexts.Add(contextName);
        }

        /// <summary>
        ///   Adds a set of context to the finder.
        /// </summary>
        /// <param name = "contextNames">The context names.</param>
        public void AddContextRangeFind(IEnumerable<string> contextNames)
        {
            foreach (var contextName in contextNames)
                AddContextFind(contextName);
        }

        /// <summary>
        ///   Clears the context finder.
        /// </summary>
        public void ClearContextFind()
        {
            CurrentContexts.Clear();
        }

        /// <summary>
        ///   Finds the first element by regex.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "regex">The regex.</param>
        /// <returns></returns>
        public T FindFirst<T>(string regex) where T : CppElement
        {
            return Find<T>(regex).FirstOrDefault();
        }

        /// <summary>
        ///   Finds the specified elements by regex.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "regex">The regex.</param>
        /// <returns></returns>
        public IEnumerable<T> Find<T>(string regex) where T : CppElement => Find<T>(CreateFullMatchRegex(regex));

        /// <summary>
        ///   Finds the specified elements by regex.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "regex">The regex.</param>
        /// <returns></returns>
        public IEnumerable<T> Find<T>(Regex regex) where T : CppElement
        {
            return Find<T>(Root, regex).OfType<T>();
        }

        public static Regex CreateFullMatchRegex(string regex)
        {
            return new Regex("^" + regex + "$");
        }

        /// <summary>
        ///   Remove recursively elements matching the regex of type T
        /// </summary>
        /// <typeparam name = "T">the T type to match</typeparam>
        /// <param name = "regex">the regex to match</param>
        public void Remove<T>(string regex) where T : CppElement => Remove<T>(CreateFullMatchRegex(regex));

        /// <summary>
        ///   Remove recursively elements matching the regex of type T
        /// </summary>
        /// <typeparam name = "T">the T type to match</typeparam>
        /// <param name = "regex">the regex to match</param>
        public void Remove<T>(Regex regex) where T : CppElement
        {
            var toRemove = Find<T>(regex).ToList();
            foreach (var item in toRemove)
            {
                item.Parent.Remove(item);
            }
        }

        /// <summary>
        ///   Modifies the specified elements by regex with the specified modifier.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "regex">The regex.</param>
        /// <param name = "modifier">The modifier.</param>
        public void Modify<T>(string regex, Action<CppElement> modifier)
            where T : CppElement => Modify<T>(CreateFullMatchRegex(regex), modifier);
        
        /// <summary>
        ///   Modifies the specified elements by regex with the specified modifier.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "regex">The regex.</param>
        /// <param name = "modifier">The modifier.</param>
        public void Modify<T>(Regex regex, Action<CppElement> modifier) where T : CppElement
        {
            foreach (var element in Find<T>(Root, regex))
            {
                modifier?.Invoke(element);
            }
        }

        /// <summary>
        ///   Strips the regex. Removes ^ and $ at the end of the string
        /// </summary>
        /// <param name = "regex">The regex.</param>
        /// <returns></returns>
        public static string StripRegex(string regex)
        {
            string friendlyRegex = regex;
            // Remove ^ and $
            if (friendlyRegex.StartsWith("^"))
                friendlyRegex = friendlyRegex.Substring(1);
            if (friendlyRegex.EndsWith("$"))
                friendlyRegex = friendlyRegex.Substring(0, friendlyRegex.Length - 1);
            return friendlyRegex;
        }

        /// <summary>
        ///   Finds all elements by regex.
        /// </summary>
        /// <param name = "regex">The regex.</param>
        /// <returns></returns>
        public IEnumerable<CppElement> FindAll(string regex)
        {
            return Find<CppElement>(regex);
        }

        /// <summary>
        /// Finds the specified elements by regex and modifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentNode">The current node in the search.</param>
        /// <param name="regex">The regex.</param>
        /// <param name="matchingElements">To add.</param>
        /// <param name="modifier">The modifier.</param>
        /// <returns></returns>
        private IEnumerable<T> Find<T>(CppElement currentNode, Regex regex) where T : CppElement
        {
            var path = currentNode.FullName;

            var elementToModify = currentNode;

            if ((elementToModify is T) && path != null && regex.Match(path).Success)
            {
                yield return (T)elementToModify;
            }

            if (currentNode == Root && CurrentContexts.Count != 0)
            {
                // Optimized version with context attributes
                foreach (var innerElement in currentNode.AllItems.Where(element => CurrentContexts.Contains(element.Name)))
                {
                    foreach (var item in Find<T>(innerElement, regex))
                    {
                        yield return item;
                    }
                }
            }
            else
            {
                foreach (var innerElement in currentNode.AllItems)
                {
                    foreach (var item in Find<T>(innerElement, regex))
                    {
                        yield return item;
                    }
                }
            }
        }

    }
}
