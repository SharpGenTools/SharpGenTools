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
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SharpGen.Runtime.Diagnostics
{
    /// <summary>
    /// Event args for <see cref="CppObject"/> used by <see cref="ObjectTracker"/>.
    /// </summary>
    public class CppObjectEventArgs : EventArgs
    {
        /// <summary>
        /// The object being tracked/untracked.
        /// </summary>
        public CppObject Object { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CppObjectEventArgs"/> class.
        /// </summary>
        /// <param name="o">The o.</param>
        public CppObjectEventArgs(CppObject o)
        {
            Object = o;
        }
    }


    /// <summary>
    /// Track all allocated objects.
    /// </summary>
    public static partial class ObjectTracker
    {
        private static Dictionary<IntPtr, List<ObjectReference>> _processGlobalObjectReferences;

        private static readonly ThreadLocal<Dictionary<IntPtr, List<ObjectReference>>> ThreadStaticObjectReferences = new(static () => new Dictionary<IntPtr, List<ObjectReference>>(), false);

        /// <summary>
        /// Occurs when a CppObject is tracked.
        /// </summary>
        public static event EventHandler<CppObjectEventArgs> Tracked;

        /// <summary>
        /// Occurs when a CppObject is untracked.
        /// </summary>
        public static event EventHandler<CppObjectEventArgs> UnTracked;

        private static Dictionary<IntPtr, List<ObjectReference>> ObjectReferences =>
            Configuration.UseThreadStaticObjectTracking
                ? ThreadStaticObjectReferences.Value
                : _processGlobalObjectReferences ??= new Dictionary<IntPtr, List<ObjectReference>>();

        /// <summary>
        /// Tracks the specified C++ object.
        /// </summary>
        /// <param name="cppObject">The C++ object.</param>
        public static void Track(CppObject cppObject)
        {
            if (cppObject == null)
                return;

            var nativePointer = cppObject.NativePointer;
            if (nativePointer == IntPtr.Zero)
                return;

            var objectReferences = ObjectReferences;

            lock (objectReferences)
                TrackImpl(cppObject, nativePointer, objectReferences);

            // Fire Tracked event.
            OnTracked(cppObject);
        }

        private static void TrackImpl(CppObject cppObject, IntPtr nativePointer, Dictionary<IntPtr, List<ObjectReference>> objectReferences)
        {
            if (!objectReferences.TryGetValue(nativePointer, out var referenceList))
            {
                referenceList = new List<ObjectReference>();
                objectReferences.Add(nativePointer, referenceList);
            }

            referenceList.Add(
                new ObjectReference(
                    DateTime.Now, cppObject, StackTraceProvider != null ? StackTraceProvider() : string.Empty
                )
            );
        }

        /// <summary>
        /// Finds a list of object reference from a specified C++ object pointer.
        /// </summary>
        /// <param name="objPtr">The C++ object pointer.</param>
        /// <returns>A list of object reference</returns>
        public static List<ObjectReference> Find(IntPtr objPtr)
        {
            lock (ObjectReferences)
            {
                // Object is already tracked
                if (ObjectReferences.TryGetValue(objPtr, out var referenceList))
                    return new List<ObjectReference>(referenceList);
            }
            return new List<ObjectReference>();
        }

        /// <summary>
        /// Finds the object reference for a specific COM object.
        /// </summary>
        /// <param name="cppObject">The COM object.</param>
        /// <returns>An object reference</returns>
        public static ObjectReference Find(CppObject cppObject) => Find(cppObject, cppObject.NativePointer);

        internal static ObjectReference Find(CppObject cppObject, IntPtr nativePointer)
        {
            lock (ObjectReferences)
            {
                if (ObjectReferences.TryGetValue(nativePointer, out var referenceList))
                {
                    foreach (var objectReference in referenceList)
                    {
                        if (ReferenceEquals(objectReference.Object.Target, cppObject))
                            return objectReference;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Untracks the specified COM object.
        /// </summary>
        /// <param name="cppObject">The COM object.</param>
        public static void UnTrack(CppObject cppObject) => Untrack(cppObject, cppObject.NativePointer);

        internal static void Untrack(CppObject cppObject, IntPtr nativePointer)
        {
            if (cppObject == null || nativePointer == IntPtr.Zero)
                return;

            var objectReferences = ObjectReferences;

            bool foundTracked;
            lock (objectReferences)
            {
                foundTracked = UntrackImpl(cppObject, nativePointer, objectReferences);
            }

            if (foundTracked)
            {
                // Fire UnTracked event
                OnUnTracked(cppObject);
            }
        }

        private static bool UntrackImpl(CppObject cppObject, IntPtr nativePointer, Dictionary<IntPtr, List<ObjectReference>> objectReferences)
        {
            var foundTracked = objectReferences.TryGetValue(nativePointer, out var referenceList);
            if (!foundTracked)
                return false;

            // Object is tracked, remove from reference list
            for (int i = referenceList.Count - 1; i >= 0; i--)
            {
                var objectReference = referenceList[i];
                if (ReferenceEquals(objectReference.Object.Target, cppObject) || !objectReference.IsAlive)
                    referenceList.RemoveAt(i);
            }

            // Remove empty list
            if (referenceList.Count == 0)
                objectReferences.Remove(nativePointer);

            return true;
        }

        internal static void MigrateNativePointer(CppObject cppObject, IntPtr oldNativePointer, IntPtr newNativePointer)
        {
            if (cppObject == null)
                return;

            var hasOldNativePointer = oldNativePointer != IntPtr.Zero;
            var hasNewNativePointer = newNativePointer != IntPtr.Zero;

            if (!hasOldNativePointer && !hasNewNativePointer)
                return;

            var objectReferences = ObjectReferences;

            lock (objectReferences)
            {
                if (hasOldNativePointer)
                    UntrackImpl(cppObject, oldNativePointer, objectReferences);
                if (hasNewNativePointer)
                    TrackImpl(cppObject, newNativePointer, objectReferences);
            }
        }

        /// <summary>
        /// Reports all COM object that are active and not yet disposed.
        /// </summary>
        public static List<ObjectReference> FindActiveObjects()
        {
            var activeObjects = new List<ObjectReference>();
            lock (ObjectReferences)
            {
                foreach (var referenceList in ObjectReferences.Values)
                {
                    foreach (var objectReference in referenceList)
                    {
                        if (objectReference.IsAlive)
                            activeObjects.Add(objectReference);
                    }
                }
            }
            return activeObjects;
        }

        /// <summary>
        /// Reports all C++ objects that are active and not yet disposed.
        /// </summary>
        public static string ReportActiveObjects()
        {
            var text = new StringBuilder();
            var count = 0;
            var countPerType = new Dictionary<string, int>();

            foreach (var findActiveObject in FindActiveObjects())
            {
                var findActiveObjectStr = findActiveObject.ToString();
                if (string.IsNullOrEmpty(findActiveObjectStr))
                    continue;

                text.AppendFormat("[{0}]: {1}", count++, findActiveObjectStr);

                var target = findActiveObject.Object.Target;
                if (target == null)
                    continue;

                var targetType = target.GetType().Name;
                countPerType[targetType] = countPerType.TryGetValue(targetType, out var typeCount)
                                               ? typeCount + 1
                                               : 1;
            }

            var keys = new List<string>(countPerType.Keys);
            keys.Sort();

            text.AppendLine();
            text.AppendLine("Count per Type:");
            foreach (var key in keys)
            {
                text.AppendFormat("{0} : {1}", key, countPerType[key]);
                text.AppendLine();
            }

            return text.ToString();
        }

        private static void OnTracked(CppObject obj)
        {
            Tracked?.Invoke(null, new CppObjectEventArgs(obj));
        }

        private static void OnUnTracked(CppObject obj)
        {
            UnTracked?.Invoke(null, new CppObjectEventArgs(obj));
        }
    }
}