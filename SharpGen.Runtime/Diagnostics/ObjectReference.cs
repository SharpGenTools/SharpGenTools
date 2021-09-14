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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SharpGen.Runtime.Diagnostics
{
    /// <summary>
    /// Contains information about a tracked native object.
    /// </summary>
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    public readonly struct ObjectReference : IEquatable<ObjectReference>
    {
        // .NET Native has issues with <...> in property backing fields in structs
        private readonly DateTime _creationTime;
        private readonly WeakReference<CppObject> _reference;
        private readonly string _stackTrace;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectReference"/> class.
        /// </summary>
        /// <param name="creationTime">The creation time.</param>
        /// <param name="cppObject">The com object to track.</param>
        /// <param name="stackTrace">The stack trace.</param>
        public ObjectReference(DateTime creationTime, CppObject cppObject, string stackTrace)
        {
            _creationTime = creationTime;
            // Creates a long weak reference to the CppObject
            _reference = new WeakReference<CppObject>(cppObject, true);
            _stackTrace = stackTrace;
        }

        /// <summary>
        /// Gets the time the object was created.
        /// </summary>
        /// <value>The creation time.</value>
        public DateTime CreationTime => _creationTime;

        /// <summary>
        /// Gets a weak reference to the tracked object.
        /// </summary>
        public WeakReference<CppObject> Object => _reference;

        /// <summary>
        /// Gets the stack trace when the track object was created.
        /// </summary>
        /// <value>The stack trace.</value>
        public string StackTrace => _stackTrace;

        public bool Equals(ObjectReference other) => Equals(Object, other.Object);
        public override bool Equals(object obj) => obj is ObjectReference other && Equals(other);
        public override int GetHashCode() => Object != null ? Object.GetHashCode() : 0;
        public static bool operator ==(ObjectReference left, ObjectReference right) => left.Equals(right);
        public static bool operator !=(ObjectReference left, ObjectReference right) => !left.Equals(right);

        public override string ToString() => Object.TryGetTarget(out var target)
                                                 ? string.Format(
                                                     CultureInfo.InvariantCulture,
                                                     "Active C++ Object: [{0}] Class: [{1}] Time [{2}] Stack:\r\n{3}\r\n",
                                                     Utilities.FormatPointer(target.NativePointer),
                                                     target.GetType().FullName,
                                                     CreationTime, StackTrace
                                                 )
                                                 : string.Empty;
    }
}