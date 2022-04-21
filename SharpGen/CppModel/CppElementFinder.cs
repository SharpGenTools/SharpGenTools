using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SharpGen.CppModel;

public sealed class CppElementFinder
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

    private CppElement Root { get; }

    private HashSet<string> CurrentContexts { get; } = new(StringComparer.InvariantCultureIgnoreCase);

    private void AddContext(string contextName)
    {
        CurrentContexts.Add(contextName);
    }

    public void AddContexts(IEnumerable<string> contextNames)
    {
        foreach (var contextName in contextNames)
            AddContext(contextName);
    }

    public void ClearCurrentContexts()
    {
        CurrentContexts.Clear();
    }

    public IEnumerable<T> Find<T>(Regex regex, SelectionMode mode = SelectionMode.MatchedElement)
        where T : CppElement
    {
        return Find<T>(Root, regex, mode);
    }

    private IEnumerable<T> Find<T>(CppElement currentNode, Regex regex, SelectionMode mode) where T : CppElement
    {
        var path = currentNode.FullName;

        var selectedElement = mode switch
        {
            SelectionMode.MatchedElement => currentNode,
            SelectionMode.Parent => currentNode.Parent,
            _ => throw new ArgumentException("Invalid selection mode.", nameof(mode))
        };

        if (path != null && selectedElement is T cppElement && regex.IsMatch(path))
        {
            yield return cppElement;
        }

        if (currentNode is not CppContainer container)
            yield break;

        var elements = container.AllItems;

        // Optimized version with context attributes
        if (currentNode == Root && CurrentContexts.Count != 0)
        {
            elements = elements.Where(element => CurrentContexts.Contains(element.Name));
        }

        foreach (var innerElement in elements)
        {
            foreach (var item in Find<T>(innerElement, regex, mode))
            {
                yield return item;
            }
        }
    }
}