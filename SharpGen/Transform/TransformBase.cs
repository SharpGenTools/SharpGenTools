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
using SharpGen.CppModel;
using SharpGen.Logging;
using SharpGen.Model;

namespace SharpGen.Transform;

/// <summary>
/// Base class to convert a C++ type to a C# type.
/// </summary>
public abstract class TransformBase<TCsElement, TCppElement>
    where TCsElement : CsBase
    where TCppElement : CppElement
{
    protected readonly Ioc Ioc;

    protected TransformBase(NamingRulesManager namingRules, Ioc ioc)
    {
        NamingRules = namingRules;
        Ioc = ioc ?? throw new ArgumentNullException(nameof(ioc));
    }

    /// <summary>
    /// Gets the naming rules manager.
    /// </summary>
    /// <value>The naming rules manager.</value>
    protected NamingRulesManager NamingRules { get; }

    /// <summary>
    /// Gets the logger for this transformation
    /// </summary>
    protected Logger Logger => Ioc.Logger;

    /// <summary>
    /// Prepares the specified C++ element to a C# element.
    /// </summary>
    /// <param name="cppElement">The C++ element.</param>
    /// <returns>The C# element created and registered to the <see cref="TransformManager"/></returns>
    public abstract TCsElement Prepare(TCppElement cppElement);

    /// <summary>
    /// Processes the specified C# element to complete the mapping process between the C++ and C# element.
    /// </summary>
    /// <param name="csElement">The C# element.</param>
    public abstract void Process(TCsElement csElement);
}