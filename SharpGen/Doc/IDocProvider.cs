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

#nullable enable

using System.Threading.Tasks;

namespace SharpGen.Doc
{
    /// <summary>
    /// An <see cref="IDocProvider"/> implementation is responsible to provide documentation to the Parser
    /// in order to feed each C++ element with an associated documentation.
    /// This is optional.
    /// A client of Parser API could provide a documentation provider
    /// in an external assembly.
    /// </summary>
    public interface IDocProvider
    {
        /// <summary>
        /// Finds the documentation for a particular C++ item.
        /// </summary>
        /// <param name="fullName">
        /// The full name.
        /// For top level elements (like struct, interfaces, enums, functions), it's the name of the element itself.
        /// For nested elements (like interface methods), the name is of the following format: "IMyInterface::MyMethod".
        /// </param>
        /// <param name="context">Environment for documenting, used to create items and subitems</param>
        /// <returns>Non-null documentation item container created by <see cref="IDocumentationContext"/></returns>
        Task<IFindDocumentationResult> FindDocumentationAsync(string fullName, IDocumentationContext context);

        /// <summary>
        /// If true, any exception thrown, or any error logged by this provider will cause the build to fail.
        /// </summary>
        /// <remarks>
        /// For providers that rely on unstable factors (networking), it is recommended to set this to <c>false</c>.
        /// However, the default choice should be <c>true</c>.
        /// </remarks>
        bool TreatFailuresAsErrors { get; }

        /// <summary>
        /// Name of the documentation provider to be presented to user when needed.
        /// </summary>
        /// <remarks>
        /// Short version. Without words like "documentation provider" or "extension".
        /// </remarks>
        string UserFriendlyName { get; }
    }
}