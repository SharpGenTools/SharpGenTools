// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Diagnostics;
using System.Linq;
using SharpGen.Config;
using SharpGen.CppModel;

namespace SharpGen.Transform;

/// <summary>
/// This class handles renaming according to conventions. Pascal case (NamingRulesManager) for global types,  
/// Camel case (namingRulesManager) for parameters.
/// </summary>
public sealed partial class NamingRulesManager
{
    /// <summary>
    /// Renames a C++ element from <see cref="originalName"/>.
    /// </summary>
    /// <param name="rootName">Name of the root to strip away.</param>
    /// <returns>The new C# name</returns>
    private static string RenameCore(string originalName, MappingRule tag, string rootName, out bool isFinal,
                                     out bool isPrematureBreak)
    {
        var name = originalName;

        // Handle Tag
        var nameModifiedByTag = false;

        if (!string.IsNullOrEmpty(tag.MappingName))
        {
            nameModifiedByTag = true;
            name = tag.MappingName;

            // Rename is tagged as final, then return the string
            if (tag.IsFinalMappingName == true)
            {
                isFinal = true;
                isPrematureBreak = false;
                return name;
            }
        }

        isFinal = false;

        if (!name.Contains("_") && name.Any(char.IsLower) && char.IsUpper(name[0]))
        {
            isPrematureBreak = true;
            return name;
        }

        isPrematureBreak = false;

        // Remove Prefix (for enums). Don't modify names that are modified by tag
        if (!nameModifiedByTag && rootName != null && originalName.StartsWith(rootName))
            name = originalName.Substring(rootName.Length, originalName.Length - rootName.Length);

        // Remove leading '_'
        return name.TrimStart('_');
    }

    /// <summary>
    /// Renames a C++ element
    /// </summary>
    /// <param name="cppElement">The C++ element.</param>
    /// <param name="rootName">Name of the root.</param>
    /// <returns>The new name</returns>
    private string RenameCore(CppElement cppElement, string rootName = null)
    {
        Debug.Assert(cppElement is not CppField and not CppParameter);

        var originalName = cppElement.Name;
        var tag = cppElement.Rule;
        var name = RenameCore(originalName, tag, rootName, out var isFinal, out var isPreempted);

        if (isFinal)
            return name;

        var namingFlags = tag.NamingFlags is { } flags ? flags : NamingFlags.Default;

        if (isPreempted && (namingFlags & NamingFlags.NoPrematureBreak) == 0)
            return name;

        // Convert rest of the string in CamelCase
        return ConvertToPascalCase(name, namingFlags);
    }

    /// <summary>
    /// Renames the specified C++ element.
    /// </summary>
    /// <param name="cppElement">The C++ element.</param>
    /// <returns>The C# name</returns>
    public string Rename(CppElement cppElement) => UnKeyword(RenameCore(cppElement));

    /// <summary>
    /// Renames the specified C++ enum item.
    /// </summary>
    /// <param name="cppEnumItem">The C++ enum item.</param>
    /// <param name="rootEnumName">Name of the root C++ enum.</param>
    /// <returns>The C# name of this enum item</returns>
    public string Rename(CppEnumItem cppEnumItem, string rootEnumName) =>
        UnKeyword(FixDigitName(RenameCore(cppEnumItem, rootEnumName), "Value"));

    private static string FixDigitName(string name, string prefix) => char.IsDigit(name[0]) ? prefix + name : name;
}