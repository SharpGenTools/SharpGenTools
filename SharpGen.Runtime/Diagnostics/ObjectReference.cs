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
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace SharpGen.Runtime.Diagnostics
{
    /// <summary>
    /// Contains information about a tracked COM object.
    /// </summary>
    public class ObjectReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectReference"/> class.
        /// </summary>
        /// <param name="creationTime">The creation time.</param>
        /// <param name="cppObject">The com object to track.</param>
        /// <param name="stackTrace">The stack trace.</param>
        public ObjectReference(DateTime creationTime, CppObject cppObject, string stackTrace)
        {
            CreationTime = creationTime;
            // Creates a long weak reference to the CppObject
            Object = new WeakReference(cppObject, true);
            StackTrace = stackTrace;
        }

        /// <summary>
        /// Gets the time the object was created.
        /// </summary>
        /// <value>The creation time.</value>
        public DateTime CreationTime { get; }

        /// <summary>
        /// Gets a weak reference to the tracked object.
        /// </summary>
        /// <value>The weak reference to the tracked object.</value>
        public WeakReference Object { get; }

        /// <summary>
        /// Gets the stack trace when the track object was created.
        /// </summary>
        /// <value>The stack trace.</value>
        public string StackTrace { get; }

        /// <summary>
        /// Gets a value indicating whether the tracked object is alive.
        /// </summary>
        /// <value><c>true</c> if tracked object is alive; otherwise, <c>false</c>.</value>
        public bool IsAlive => Object.IsAlive;

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var cppObject = Object.Target as CppObject;
            if (cppObject == null)
                return "";

            var builder = new StringBuilder();
            builder.AppendFormat(CultureInfo.InvariantCulture, "Active C++ Object: [0x{0:X}] Class: [{1}] Time [{2}] Stack:\r\n{3}", cppObject.NativePointer.ToInt64(), cppObject.GetType().FullName, CreationTime, StackTrace).AppendLine();
            return builder.ToString();
        }
    }
        
}