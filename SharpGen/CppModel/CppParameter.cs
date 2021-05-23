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

using SharpGen.Config;

namespace SharpGen.CppModel
{
    public sealed class CppParameter : CppMarshallable
    {
        private ParamAttribute attribute;

        public ParamAttribute Attribute
        {
            get => CoerceAttribute(
                Rule.ParameterAttribute switch
                {
                    { } paramAttributeValue => paramAttributeValue,
                    _ => attribute
                }
            );
            set => attribute = value;
        }

        private static ParamAttribute CoerceAttribute(ParamAttribute value)
        {
            const ParamAttribute inOutMask = ParamAttribute.In | ParamAttribute.InOut | ParamAttribute.Out;

            // Parameters without In/Out annotations are considered as In
            return (value & inOutMask) == 0 ? value | ParamAttribute.In : value;
        }

        internal bool IsAttributeRuleRedundant => Rule.ParameterAttribute is { } ruleValue
                                               && CoerceAttribute(ruleValue) == CoerceAttribute(attribute);

        public override string ToString() => "[" + Attribute + "] " + base.ToString();

        public CppParameter(string name) : base(name)
        {
        }
    }
}