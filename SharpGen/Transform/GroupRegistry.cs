using System.Collections.Generic;
using SharpGen.Model;

namespace SharpGen.Transform;

public class GroupRegistry
{
    private readonly Dictionary<string, CsGroup> _mapRegisteredFunctionGroup = new();

    public void RegisterGroup(string className, CsGroup group)
    {
        _mapRegisteredFunctionGroup.Add(className, group);
    }

    /// <summary>
    /// Finds a C# class container by name.
    /// </summary>
    /// <param name="className">Name of the class.</param>
    /// <returns></returns>
    public CsGroup FindGroup(string className)
    {
        _mapRegisteredFunctionGroup.TryGetValue(className, out var csClass);
        return csClass;
    }
}