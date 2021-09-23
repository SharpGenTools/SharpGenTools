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
    using ObjectReferenceDictionary = Dictionary<IntPtr, List<ObjectReference>>;

    /// <summary>
    /// Track all allocated objects.
    /// </summary>
    public static class ObjectTracker
    {
        private static ObjectReferenceDictionary _processGlobalObjectReferences;
        private static readonly ThreadLocal<ObjectReferenceDictionary> ThreadStaticObjectReferences = new(static () => new ObjectReferenceDictionary(), false);

        /// <summary>
        /// Occurs when a CppObject is tracked.
        /// </summary>
        public static event Action<CppObject> Tracked;

        /// <summary>
        /// Occurs when a CppObject is untracked.
        /// </summary>
        public static event Action<CppObject> UnTracked;

        private static ObjectReferenceDictionary ObjectReferences =>
            ObjectTrackerReadOnlyConfiguration.IsObjectTrackingThreadStatic
                ? ThreadStaticObjectReferences.Value
                : _processGlobalObjectReferences ??= new();

        /// <summary>
        /// Tracks the specified native object.
        /// </summary>
        /// <param name="cppObject">The native object.</param>
        public static void Track(CppObject cppObject)
        {
            if (cppObject is null)
                return;

            var nativePointer = cppObject.NativePointer;

            Track(cppObject, nativePointer);
        }

        internal static void Track(CppObject cppObject, IntPtr nativePointer)
        {
            if (nativePointer == IntPtr.Zero)
                return;

            var objectReferences = ObjectReferences;

            lock (objectReferences)
                TrackImpl(cppObject, nativePointer, objectReferences);

            // Fire an event.
            Tracked?.Invoke(cppObject);
        }

        private static void TrackImpl(CppObject cppObject, IntPtr nativePointer, ObjectReferenceDictionary objectReferences)
        {
            if (!objectReferences.TryGetValue(nativePointer, out var referenceList))
            {
                referenceList = new List<ObjectReference>();
                objectReferences.Add(nativePointer, referenceList);
            }

            referenceList.Add(new ObjectReference(DateTime.Now, cppObject, Environment.StackTrace));
        }

        /// <summary>
        /// Finds a list of object reference from a specified C++ object pointer.
        /// </summary>
        /// <param name="objPtr">The C++ object pointer.</param>
        /// <returns>A list of object reference</returns>
        public static List<ObjectReference> Find(IntPtr objPtr)
        {
            var objectReferences = ObjectReferences;
            lock (objectReferences)
            {
                // Object is already tracked
                if (objectReferences.TryGetValue(objPtr, out var referenceList))
                    return new List<ObjectReference>(referenceList);
            }

            return new List<ObjectReference>();
        }

        /// <summary>
        /// Untracks the specified native object.
        /// </summary>
        /// <param name="cppObject">The native object.</param>
        public static void UnTrack(CppObject cppObject)
        {
            if (cppObject is null)
                return;

            var nativePointer = cppObject.NativePointer;

            Untrack(cppObject, nativePointer);
        }

        internal static void Untrack(CppObject cppObject, IntPtr nativePointer)
        {
            if (nativePointer == IntPtr.Zero)
                return;

            var objectReferences = ObjectReferences;

            bool foundTracked;
            lock (objectReferences)
            {
                foundTracked = UntrackImpl(cppObject, nativePointer, objectReferences);
            }

            if (foundTracked)
            {
                // Fire an event
                UnTracked?.Invoke(cppObject);
            }
        }

        private static bool UntrackImpl(CppObject cppObject, IntPtr nativePointer, ObjectReferenceDictionary objectReferences)
        {
            var foundTracked = objectReferences.TryGetValue(nativePointer, out var referenceList);
            if (!foundTracked)
                return false;

            // Object is tracked, remove from reference list
            for (var i = referenceList.Count - 1; i >= 0; --i)
            {
                var objectReference = referenceList[i].Object;
                if (!objectReference.TryGetTarget(out var target) || ReferenceEquals(target, cppObject))
                    referenceList.RemoveAt(i);
            }

            // Remove empty list
            if (referenceList.Count == 0)
                objectReferences.Remove(nativePointer);

            return true;
        }

        internal static void MigrateNativePointer(CppObject cppObject, IntPtr oldNativePointer, IntPtr newNativePointer)
        {
            if (cppObject is null)
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
            List<ObjectReference> activeObjects;
            var objectReferences = ObjectReferences;
            lock (objectReferences)
            {
                activeObjects = new(objectReferences.Count);
                foreach (var referenceList in objectReferences.Values)
                {
                    foreach (var objectReference in referenceList)
                    {
                        if (objectReference.Object.TryGetTarget(out _))
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

                if (!findActiveObject.Object.TryGetTarget(out var target))
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
    }
}