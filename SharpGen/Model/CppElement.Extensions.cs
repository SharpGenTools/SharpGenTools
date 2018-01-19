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
using System;
using System.Text.RegularExpressions;
using SharpGen.Config;
using SharpGen.CppModel;
using System.Reflection;

namespace SharpGen.Model
{
    public static class CppElementExtensions
    {

        private static string LastCppOuterElement = "???";

        public static void ExecuteRule<T>(this CppElement element, string regex, MappingRule rule) where T : CppElement
        {
            var regexStr = CppElement.StripRegex(regex);
            if (typeof(CppMethod).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())
                || typeof(CppStruct).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()))
            {
                LastCppOuterElement = regexStr;
            }
            else if ( (typeof(T) == typeof(CppParameter) || typeof(T) == typeof(CppField)) && !regexStr.Contains("::"))
            {
                regexStr = LastCppOuterElement + "::" + regexStr;
            }
            
            element.Modify<T>(regexStr, ProcessRule(rule));
        }

        public static string GetTypeNameWithMapping(this CppElement cppType)
        {
            var rule = cppType.GetMappingRule();
            if (rule != null && rule.MappingType != null)
                return rule.MappingType;
            if (cppType is CppEnum cppEnum)
                return cppEnum.UnderlyingType;
            if (cppType is CppMarshallable type)
                return type.TypeName;
            throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Cannot get type name from type {0}", cppType));
        }

        private static string RegexRename(Regex regex, string fromName, string replaceName)
        {
            return replaceName.Contains("$")? regex.Replace(fromName, replaceName) : replaceName;            
        }

        /// <summary>
        ///   Fully rename a type and all references
        /// </summary>
        /// <param name = "newRule"></param>
        /// <returns></returns>
        private static CppElement.ProcessModifier ProcessRule(MappingRule newRule)
        {
            return (patchRegex, element) =>
                {
                    var tag = element.Rule;
                    if (tag == null)
                    {
                        element.Rule = tag = new MappingRule();
                    }
                    if (newRule.Assembly != null) tag.Assembly = newRule.Assembly;
                    if (newRule.Namespace != null) tag.Namespace = newRule.Namespace;
                    if (newRule.DefaultValue != null) tag.DefaultValue = newRule.DefaultValue;
                    if (newRule.MethodCheckReturnType.HasValue) tag.MethodCheckReturnType = newRule.MethodCheckReturnType;
                    if (newRule.AlwaysReturnHResult.HasValue) tag.AlwaysReturnHResult = newRule.AlwaysReturnHResult;
                    if (newRule.RawPtr.HasValue) tag.RawPtr = newRule.RawPtr;
                    if (newRule.Visibility.HasValue) tag.Visibility = newRule.Visibility;
                    if (newRule.NativeCallbackVisibility.HasValue) tag.NativeCallbackVisibility = newRule.NativeCallbackVisibility;
                    if (newRule.NativeCallbackName != null) tag.NativeCallbackName = newRule.NativeCallbackName;
                    if (newRule.Property.HasValue) tag.Property = newRule.Property;
                    if (newRule.CustomVtbl.HasValue) tag.CustomVtbl = newRule.CustomVtbl;
                    if (newRule.Persist.HasValue) tag.Persist = newRule.Persist;
                    if (newRule.Replace != null) tag.Replace = newRule.Replace;
                    if (newRule.MappingName != null) 
                        tag.MappingName = RegexRename(patchRegex, element.FullName, newRule.MappingName);
                    if (newRule.NamingFlags.HasValue) tag.NamingFlags = newRule.NamingFlags.Value;
                    if (newRule.IsFinalMappingName != null) tag.IsFinalMappingName = newRule.IsFinalMappingName;
                    if (newRule.StructPack != null) tag.StructPack = newRule.StructPack;
                    if (newRule.StructHasNativeValueType != null) tag.StructHasNativeValueType = newRule.StructHasNativeValueType;
                    if (newRule.StructToClass != null) tag.StructToClass = newRule.StructToClass;
                    if (newRule.StructCustomMarshal != null) tag.StructCustomMarshal = newRule.StructCustomMarshal;
                    if (newRule.StructCustomNew != null) tag.StructCustomNew = newRule.StructCustomNew;
                    if (newRule.StructForceMarshalToToBeGenerated != null)
                        tag.StructForceMarshalToToBeGenerated = newRule.StructForceMarshalToToBeGenerated;
                    if (newRule.MappingType != null) tag.MappingType = RegexRename(patchRegex, element.FullName, newRule.MappingType);

                    if (element is CppMarshallable cppType)
                    {
                        if (newRule.MappingType != null)
                            tag.MappingType = newRule.MappingType;

                        if (newRule.Pointer != null)
                        {
                            cppType.Pointer = newRule.Pointer;
                            tag.Pointer = newRule.Pointer;
                        }
                        if (newRule.TypeArrayDimension != null)
                        {
                            cppType.ArrayDimension = newRule.TypeArrayDimension;
                            if (newRule.TypeArrayDimension == null)
                                cppType.IsArray = false;
                            tag.TypeArrayDimension = newRule.TypeArrayDimension;
                        }
                    }
                    if (newRule.EnumHasFlags != null) tag.EnumHasFlags = newRule.EnumHasFlags;
                    if (newRule.EnumHasNone != null) tag.EnumHasNone = newRule.EnumHasNone;
                    if (newRule.IsCallbackInterface != null) tag.IsCallbackInterface = newRule.IsCallbackInterface;
                    if (newRule.IsDualCallbackInterface != null) tag.IsDualCallbackInterface = newRule.IsDualCallbackInterface;
                    if (newRule.IsKeepImplementPublic != null) tag.IsKeepImplementPublic = newRule.IsKeepImplementPublic;
                    if (newRule.FunctionDllName != null) tag.FunctionDllName = RegexRename(patchRegex, element.FullName, newRule.FunctionDllName);
                    if (newRule.Group != null) tag.Group = newRule.Group;
                    if (newRule.ParameterAttribute != null && element is CppParameter param)
                    {
                        param.Attribute = newRule.ParameterAttribute.Value;
                        tag.ParameterAttribute = newRule.ParameterAttribute.Value;
                    }
                    if (newRule.ParameterUsedAsReturnType != null ) tag.ParameterUsedAsReturnType = newRule.ParameterUsedAsReturnType;
                    return false;
                };
        }
    }
}