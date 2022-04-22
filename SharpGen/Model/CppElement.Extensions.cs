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

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using SharpGen.Config;
using SharpGen.CppModel;

namespace SharpGen.Model;

public static class CppElementExtensions
{
    /// <summary>
    ///   Finds the specified elements by regex.
    /// </summary>
    /// <typeparam name = "T"></typeparam>
    /// <param name = "regex">The regex.</param>
    /// <param name="finder">The C++ element finder instance to use.</param>
    /// <param name="mode">The selection mode for selecting matched elements.</param>
    /// <returns></returns>
    public static IEnumerable<T> Find<T>(
        this CppElementFinder finder,
        string regex,
        CppElementFinder.SelectionMode mode = CppElementFinder.SelectionMode.MatchedElement)
        where T : CppElement
        => finder.Find<T>(BuildFindFullRegex(regex), mode);

    /// <summary>
    ///   Strips the regex. Removes ^ and $ at the end of the string
    /// </summary>
    /// <param name = "regex">The regex.</param>
    /// <returns></returns>
    internal static Regex BuildFindFullRegex(string regex)
    {
        StringBuilder sb = new(regex.Length + 2);
        if (!regex.StartsWith("^"))
            sb.Append('^');
        sb.Append(regex);
        if (!regex.EndsWith("$"))
            sb.Append('$');
        return new Regex(sb.ToString());
    }

    public static bool ExecuteRule<T>(this CppElementFinder finder, string regex, MappingRule rule)
        where T : CppElement
    {
        var mode = CppElementFinder.SelectionMode.MatchedElement;

        var matchedAny = false;

        if (regex.StartsWith("#"))
        {
            mode = CppElementFinder.SelectionMode.Parent;
            regex = regex.Substring(1);
        }

        var fullRegex = BuildFindFullRegex(regex);

        foreach (var item in finder.Find<T>(fullRegex, mode))
        {
            matchedAny = true;
            ProcessRule(item, rule, fullRegex);
        }

        return matchedAny;
    }

    private static string RegexRename(Regex regex, string fromName, string replaceName)
    {
        return replaceName.Contains("$") ? regex.Replace(fromName, replaceName) : replaceName;
    }

    /// <summary>
    ///   Fully rename a type and all references
    /// </summary>
    /// <param name = "newRule"></param>
    /// <returns></returns>
    private static void ProcessRule(CppElement element, MappingRule newRule, Regex patchRegex)
    {
        var tag = element.Rule;

        if (newRule.Namespace != null) tag.Namespace = newRule.Namespace;
        if (newRule.DefaultValue != null) tag.DefaultValue = newRule.DefaultValue;
        if (newRule.MethodCheckReturnType.HasValue) tag.MethodCheckReturnType = newRule.MethodCheckReturnType;
        if (newRule.AlwaysReturnHResult.HasValue) tag.AlwaysReturnHResult = newRule.AlwaysReturnHResult;
        if (newRule.RawPtr.HasValue) tag.RawPtr = newRule.RawPtr;
        if (newRule.Visibility.HasValue) tag.Visibility = newRule.Visibility;
        if (newRule.NativeCallbackVisibility.HasValue)
            tag.NativeCallbackVisibility = newRule.NativeCallbackVisibility;
        if (newRule.ShadowVisibility.HasValue)
            tag.ShadowVisibility = newRule.ShadowVisibility;
        if (newRule.VtblVisibility.HasValue)
            tag.VtblVisibility = newRule.VtblVisibility;
        if (newRule.NativeCallbackName != null)
            tag.NativeCallbackName = RegexRename(patchRegex, element.FullName, newRule.NativeCallbackName);
        if (newRule.Property.HasValue) tag.Property = newRule.Property;
        if (newRule.CustomVtbl.HasValue) tag.CustomVtbl = newRule.CustomVtbl;
        if (newRule.Persist.HasValue) tag.Persist = newRule.Persist;
        if (newRule.MappingName is { } mappingName)
            tag.MappingName = RegexRename(patchRegex, element.FullName, mappingName);
        if (newRule.MappingNameFinal is { } mappingNameFinal)
            tag.MappingNameFinal = RegexRename(patchRegex, element.FullName, mappingNameFinal);
        if (newRule.NamingFlags.HasValue) tag.NamingFlags = newRule.NamingFlags.Value;
        if (newRule.StructPack != null) tag.StructPack = newRule.StructPack;
        if (newRule.StructHasNativeValueType != null)
            tag.StructHasNativeValueType = newRule.StructHasNativeValueType;
        if (newRule.StructToClass != null) tag.StructToClass = newRule.StructToClass;
        if (newRule.StructCustomMarshal != null) tag.StructCustomMarshal = newRule.StructCustomMarshal;
        if (newRule.StructCustomNew != null) tag.StructCustomNew = newRule.StructCustomNew;
        if (newRule.IsStaticMarshal != null) tag.IsStaticMarshal = newRule.IsStaticMarshal;
        if (newRule.MappingType != null)
            tag.MappingType = RegexRename(patchRegex, element.FullName, newRule.MappingType);
        if (newRule.OverrideNativeType != null) tag.OverrideNativeType = newRule.OverrideNativeType;
        if (newRule.Pointer is { } pointer) tag.Pointer = pointer;
        if (newRule.TypeArrayDimension is { } arrayDimension) tag.TypeArrayDimension = arrayDimension;
        if (newRule.EnumHasFlags != null) tag.EnumHasFlags = newRule.EnumHasFlags;
        if (newRule.EnumHasNone != null) tag.EnumHasNone = newRule.EnumHasNone;
        if (newRule.IsCallbackInterface != null) tag.IsCallbackInterface = newRule.IsCallbackInterface;
        if (newRule.IsDualCallbackInterface != null) tag.IsDualCallbackInterface = newRule.IsDualCallbackInterface;
        if (newRule.AutoGenerateShadow != null) tag.AutoGenerateShadow = newRule.AutoGenerateShadow;
        if (newRule.AutoGenerateVtbl != null) tag.AutoGenerateVtbl = newRule.AutoGenerateVtbl;
        if (newRule.StaticShadowVtbl != null) tag.StaticShadowVtbl = newRule.StaticShadowVtbl;
        if (newRule.AutoDisposePersistentProperties is { } autoDisposePersistentProperties)
            tag.AutoDisposePersistentProperties = autoDisposePersistentProperties;
        if (newRule.ShadowName != null)
            tag.ShadowName = RegexRename(patchRegex, element.FullName, newRule.ShadowName);
        if (newRule.VtblName != null) tag.VtblName = RegexRename(patchRegex, element.FullName, newRule.VtblName);
        if (newRule.IsKeepImplementPublic != null) tag.IsKeepImplementPublic = newRule.IsKeepImplementPublic;
        if (newRule.FunctionDllName != null)
            tag.FunctionDllName = RegexRename(patchRegex, element.FullName, newRule.FunctionDllName);
        if (newRule.Group != null) tag.Group = newRule.Group;
        if (newRule.ParameterAttribute is { } paramAttributeValue) tag.ParameterAttribute = paramAttributeValue;
        if (newRule.ParameterUsedAsReturnType != null)
            tag.ParameterUsedAsReturnType = newRule.ParameterUsedAsReturnType;
        if (newRule.Relation != null) tag.Relation = newRule.Relation;
        if (newRule.Hidden != null) tag.Hidden = newRule.Hidden;
        if (newRule.KeepPointers != null) tag.KeepPointers = newRule.KeepPointers;
        if (newRule.StringMarshal is { } stringMarshal) tag.StringMarshal = stringMarshal;
    }
}