using SharpGen.CppModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpGen.CppModel
{
    public class CppElementFinder
    {
        public enum SelectionMode
        {
            MatchedElement,
            Parent
        }

        public CppElementFinder(CppElement root)
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
        public void AddContext(string contextName)
        {
            CurrentContexts.Add(contextName);
        }

        /// <summary>
        ///   Adds a set of context to the finder.
        /// </summary>
        /// <param name = "contextNames">The context names.</param>
        public void AddContexts(IEnumerable<string> contextNames)
        {
            foreach (var contextName in contextNames)
                AddContext(contextName);
        }

        /// <summary>
        ///   Clears the context finder.
        /// </summary>
        public void ClearCurrentContexts()
        {
            CurrentContexts.Clear();
        }

        /// <summary>
        ///   Finds the specified elements by regex.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "regex">The regex.</param>
        /// <param name="mode">The selection mode.</param>
        /// <returns>The selected elements.</returns>
        public IEnumerable<T> Find<T>(Regex regex, SelectionMode mode = SelectionMode.MatchedElement)
            where T : CppElement
        {
            return Find<T>(Root, regex, mode).OfType<T>();
        }

        /// <summary>
        /// Finds the specified elements by regex.
        /// </summary>
        /// <typeparam name="T">The type of element to find</typeparam>
        /// <param name="currentNode">The current node in the search.</param>
        /// <param name="regex">The regex.</param>
        /// <param name="mode">The selection mode.</param>
        /// <returns>The selected elements.</returns>
        private IEnumerable<T> Find<T>(CppElement currentNode, Regex regex, SelectionMode mode) where T : CppElement
        {
            var path = currentNode.FullName;

            CppElement selectedElement;

            switch (mode)
            {
                case SelectionMode.MatchedElement:
                    selectedElement = currentNode;
                    break;
                case SelectionMode.Parent:
                    selectedElement = currentNode.Parent;
                    break;
                default:
                    throw new ArgumentException("Invalid selection mode.", nameof(mode));
            }

            if (path != null && (selectedElement is T cppElement) && regex.Match(path).Success)
            {
                yield return cppElement;
            }

            if (currentNode == Root && CurrentContexts.Count != 0)
            {
                // Optimized version with context attributes
                foreach (var innerElement in currentNode.AllItems.Where(element => CurrentContexts.Contains(element.Name)))
                {
                    foreach (var item in Find<T>(innerElement, regex, mode))
                    {
                        yield return item;
                    }
                }
            }
            else
            {
                foreach (var innerElement in currentNode.AllItems)
                {
                    foreach (var item in Find<T>(innerElement, regex, mode))
                    {
                        yield return item;
                    }
                }
            }
        }

    }
}
