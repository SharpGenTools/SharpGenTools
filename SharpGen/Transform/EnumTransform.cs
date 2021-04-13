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
using System.Linq;
using SharpGen.Logging;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;

namespace SharpGen.Transform
{
    /// <summary>
    /// Transforms a C++ enum to a C# enum definition.
    /// </summary>
    public class EnumTransform : TransformBase<CsEnum, CppEnum>, ITransformPreparer<CppEnum, CsEnum>, ITransformer<CsEnum>
    {
        private readonly TypeRegistry typeRegistry;
        private readonly NamespaceRegistry namespaceRegistry;

        public EnumTransform(
            NamingRulesManager namingRules,
            Logger logger, 
            NamespaceRegistry namespaceRegistry,
            TypeRegistry typeRegistry)
            : base(namingRules, logger)
        {
            this.namespaceRegistry = namespaceRegistry;
            this.typeRegistry = typeRegistry;
        }

        private static string GetTypeNameWithMapping(CppEnum cppEnum)
        {
            var rule = cppEnum.Rule;
            return rule is {MappingType: { } mapType} ? mapType : cppEnum.UnderlyingType;
        }

        /// <summary>
        /// Prepares the specified C++ element to a C# element.
        /// </summary>
        /// <param name="cppEnum">The C++ element.</param>
        /// <returns>The C# element created and registered to the <see cref="TransformManager"/></returns>
        public override CsEnum Prepare(CppEnum cppEnum)
        {
            // Determine enum type. Default is int
            var typeName = GetTypeNameWithMapping(cppEnum);
            var underlyingType = TypeRegistry.ImportPrimitiveType(typeName);

            if (underlyingType == null)
            {
                Logger.Error(LoggingCodes.InvalidUnderlyingType, "Invalid type [{0}] for enum [{1}]", typeName, cppEnum);
                return null;
            }

            // Create C# enum
            CsEnum newEnum = new(cppEnum, NamingRules.Rename(cppEnum), underlyingType);

            // Get the namespace for this particular include and enum
            var nameSpace = namespaceRegistry.ResolveNamespace(cppEnum);
            nameSpace.Add(newEnum);

            // Bind C++ enum to C# enum
            typeRegistry.BindType(cppEnum.Name, newEnum, source: cppEnum.ParentInclude?.Name);

            return newEnum;
        }

        /// <summary>
        /// Maps a C++ Enum to a C# enum.
        /// </summary>
        /// <param name="newEnum">the C# enum.</param>
        public override void Process(CsEnum newEnum)
        {
            var cppEnum = (CppEnum) newEnum.CppElement;

            // Find Root Name of this enum
            // All enum items should start with the same root name and the root name should be at least 4 chars
            string rootName = cppEnum.Name;
            string rootNameFound = null;
            bool isRootNameFound = false;
            for (int i = rootName.Length; i >= 4 && !isRootNameFound; i--)
            {
                rootNameFound = rootName.Substring(0, i);

                isRootNameFound = true;
                foreach (var cppEnumItem in cppEnum.EnumItems)
                {
                    if (!cppEnumItem.Name.StartsWith(rootNameFound))
                    {
                        isRootNameFound = false;
                        break;
                    }
                }
            }
            if (isRootNameFound)
                rootName = rootNameFound;

            // Create enum items for enum
            foreach (var cppEnumItem in cppEnum.EnumItems)
            {
                var enumName = NamingRules.Rename(cppEnumItem, rootName);
                var enumValue = cppEnumItem.Value;

                var csharpEnumItem = new CsEnumItem(cppEnumItem, enumName, enumValue);

                newEnum.Add(csharpEnumItem);
            }

            var rule = cppEnum.Rule;

            // Add None if necessary
            const string noneElementName = "None";

            bool tryToAddNone;
            if (rule.EnumHasNone is { } addNone)
                tryToAddNone = addNone;
            else if (newEnum.IsFlag)
                tryToAddNone = newEnum.EnumItems.All(item => item.Name != noneElementName);
            else
                tryToAddNone = false;

            if (tryToAddNone)
            {
                var csharpEnumItem = new CsEnumItem(null, noneElementName, "0")
                {
                    CppElementName = noneElementName,
                    Description = noneElementName
                };
                newEnum.Add(csharpEnumItem);
            }
        }
    }
}