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
using System.IO;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime.Win32
{
    internal class ComStreamShadow : ComStreamBaseShadow
    {
        protected override CppObjectVtbl Vtbl { get; } = new ComStreamVtbl();

        /// <summary>
        /// Callbacks to pointer.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        public static IntPtr ToIntPtr(IStream stream)
        {
            return ToCallbackPtr<IStream>(stream);
        }

        private class ComStreamVtbl : ComStreamBaseVtbl
        {
            public ComStreamVtbl() : base(9)
            {
                AddMethod(new SeekDelegate(SeekImpl));
                AddMethod(new SetSizeDelegate(SetSizeImpl));
                AddMethod(new CopyToDelegate(CopyToImpl));
                AddMethod(new CommitDelegate(CommitImpl));
                AddMethod(new RevertDelegate(RevertImpl));
                AddMethod(new LockRegionDelegate(LockRegionImpl));
                AddMethod(new UnlockRegionDelegate(UnlockRegionImpl));
                AddMethod(new StatDelegate(StatImpl));
                AddMethod(new CloneDelegate(CloneImpl));
            }

            /// <unmanaged>HRESULT IStream::Seek([In] LARGE_INTEGER dlibMove,[In] SHARPDX_SEEKORIGIN dwOrigin,[Out, Optional] ULARGE_INTEGER* plibNewPosition)</unmanaged>	
            /* public long Seek(long dlibMove, System.IO.SeekOrigin dwOrigin) */
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            private unsafe delegate int SeekDelegate(IntPtr thisPtr, long offset, SeekOrigin origin, IntPtr newPosition);
            private unsafe static int SeekImpl(IntPtr thisPtr, long offset, SeekOrigin origin, IntPtr newPosition)
            {
                try
                {
                    var shadow = ToShadow<ComStreamShadow>(thisPtr);
                    var callback = ((IStream)shadow.Callback);
                    ulong position = callback.Seek(offset, origin);

                    // pointer can be null, so we need to test it
                    if (newPosition != IntPtr.Zero)
                    {
                         *(ulong*)newPosition = position;
                    }
                }
                catch (Exception exception)
                {
                    return (int)Result.GetResultFromException(exception);
                }
                return Result.Ok.Code;
            }

            /// <unmanaged>HRESULT IStream::SetSize([In] ULARGE_INTEGER libNewSize)</unmanaged>	
            /* public SharpDX.Result SetSize(long libNewSize) */
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            private delegate Result SetSizeDelegate(IntPtr thisPtr, ulong newSize);
            private static Result SetSizeImpl(IntPtr thisPtr, ulong newSize)
            {
                var result = Result.Ok;
                try
                {
                    var shadow = ToShadow<ComStreamShadow>(thisPtr);
                    var callback = ((IStream)shadow.Callback);
                    callback.SetSize(newSize);
                }
                catch (SharpGenException exception)
                {
                    result = exception.ResultCode;
                }
                catch (Exception)
                {
                    result = Result.Fail.Code;
                }
                return result;
            }

            /// <unmanaged>HRESULT IStream::CopyTo([In] IStream* pstm,[In] ULARGE_INTEGER cb,[Out, Optional] ULARGE_INTEGER* pcbRead,[Out, Optional] ULARGE_INTEGER* pcbWritten)</unmanaged>	
            /* internal long CopyTo_(System.IntPtr stmRef, long cb, out long cbWrittenRef) */
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            private delegate int CopyToDelegate(IntPtr thisPtr, IntPtr streamPointer, ulong numberOfBytes, out ulong numberOfBytesRead, out ulong numberOfBytesWritten);
            private static int CopyToImpl(IntPtr thisPtr, IntPtr streamPointer, ulong numberOfBytes, out ulong numberOfBytesRead, out ulong numberOfBytesWritten)
            {
                numberOfBytesRead = 0;
                numberOfBytesWritten = 0;
                try
                {
                    var shadow = ToShadow<ComStreamShadow>(thisPtr);
                    var callback = ((IStream)shadow.Callback);
                    numberOfBytesRead = callback.CopyTo(new ComStream(streamPointer), numberOfBytes, out numberOfBytesWritten);
                }
                catch (Exception exception)
                {
                    return (int)Result.GetResultFromException(exception);
                }
                return Result.Ok.Code;
            }

            /// <unmanaged>HRESULT IStream::Commit([In] STGC grfCommitFlags)</unmanaged>	
            /* public SharpDX.Result Commit(SharpDX.Win32.CommitFlags grfCommitFlags) */
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            private delegate Result CommitDelegate(IntPtr thisPtr, CommitFlags flags);
            private static Result CommitImpl(IntPtr thisPtr, CommitFlags flags)
            {
                var result = Result.Ok;
                try
                {
                    var shadow = ToShadow<ComStreamShadow>(thisPtr);
                    var callback = ((IStream)shadow.Callback);
                    callback.Commit(flags);
                }
                catch (SharpGenException exception)
                {
                    result = exception.ResultCode;
                }
                catch (Exception)
                {
                    result = Result.Fail.Code;
                }
                return result;
            }

            /// <unmanaged>HRESULT IStream::Revert()</unmanaged>	
            /* public SharpDX.Result Revert() */
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            private delegate Result RevertDelegate(IntPtr thisPtr);
            private static Result RevertImpl(IntPtr thisPtr)
            {
                var result = Result.Ok;
                try
                {
                    var shadow = ToShadow<ComStreamShadow>(thisPtr);
                    var callback = ((IStream)shadow.Callback);
                    callback.Revert();
                }
                catch (SharpGenException exception)
                {
                    result = exception.ResultCode;
                }
                catch (Exception)
                {
                    result = Result.Fail.Code;
                }
                return result;
            }

            /// <unmanaged>HRESULT IStream::LockRegion([In] ULARGE_INTEGER libOffset,[In] ULARGE_INTEGER cb,[In] LOCKTYPE dwLockType)</unmanaged>	
            /* public SharpDX.Result LockRegion(long libOffset, long cb, SharpDX.Win32.LockType dwLockType) */
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            private delegate Result LockRegionDelegate(IntPtr thisPtr, ulong offset, ulong numberOfBytes, LockType lockType);
            private static Result LockRegionImpl(IntPtr thisPtr, ulong offset, ulong numberOfBytes, LockType lockType)
            {
                var result = Result.Ok;
                try
                {
                    var shadow = ToShadow<ComStreamShadow>(thisPtr);
                    var callback = ((IStream)shadow.Callback);
                    callback.LockRegion(offset, numberOfBytes, lockType);
                }
                catch (SharpGenException exception)
                {
                    result = exception.ResultCode;
                }
                catch (Exception)
                {
                    result = Result.Fail.Code;
                }
                return result;
            }


            /// <unmanaged>HRESULT IStream::UnlockRegion([In] ULARGE_INTEGER libOffset,[In] ULARGE_INTEGER cb,[In] LOCKTYPE dwLockType)</unmanaged>	
            /* public SharpDX.Result UnlockRegion(long libOffset, long cb, SharpDX.Win32.LockType dwLockType) */
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            private delegate Result UnlockRegionDelegate(IntPtr thisPtr, ulong offset, ulong numberOfBytes, LockType lockType);
            private static Result UnlockRegionImpl(IntPtr thisPtr, ulong offset, ulong numberOfBytes, LockType lockType)
            {
                var result = Result.Ok;
                try
                {
                    var shadow = ToShadow<ComStreamShadow>(thisPtr);
                    var callback = ((IStream)shadow.Callback);
                    callback.UnlockRegion(offset, numberOfBytes, lockType);
                }
                catch (SharpGenException exception)
                {
                    result = exception.ResultCode;
                }
                catch (Exception)
                {
                    result = Result.Fail.Code;
                }
                return result;
            }

            /// <unmanaged>HRESULT IStream::Stat([Out] STATSTG* pstatstg,[In] STATFLAG grfStatFlag)</unmanaged>	
            /* public SharpDX.Win32.StorageStatistics GetStatistics(SharpDX.Win32.StorageStatisticsFlags grfStatFlag) */
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            private delegate Result StatDelegate(IntPtr thisPtr, ref StorageStatistics.__Native statisticsPtr, StorageStatisticsFlags flags);
            private static Result StatImpl(IntPtr thisPtr, ref StorageStatistics.__Native statisticsPtr, StorageStatisticsFlags flags)
            {
                try
                {
                    var shadow = ToShadow<ComStreamShadow>(thisPtr);
                    var callback = ((IStream)shadow.Callback);
                    var statistics = callback.GetStatistics(flags);
                    statistics.__MarshalTo(ref statisticsPtr);
                }
                catch (SharpGenException exception)
                {
                    return exception.ResultCode;
                }
                catch (Exception)
                {
                    return Result.Fail.Code;
                }
                return Result.Ok;
            }

            /// <unmanaged>HRESULT IStream::Clone([Out] IStream** ppstm)</unmanaged>	
            /* public SharpDX.Win32.IStream Clone() */
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            private delegate Result CloneDelegate(IntPtr thisPtr, out IntPtr streamPointer);
            private static Result CloneImpl(IntPtr thisPtr, out IntPtr streamPointer)
            {
                streamPointer = IntPtr.Zero;
                var result = Result.Ok;
                try
                {
                    var shadow = ToShadow<ComStreamShadow>(thisPtr);
                    var callback = ((IStream)shadow.Callback);
                    var clone = callback.Clone();
                    streamPointer = ComStream.ToIntPtr(clone);
                }
                catch (SharpGenException exception)
                {
                    result = exception.ResultCode;
                }
                catch (Exception)
                {
                    result = Result.Fail.Code;
                }
                return result;
            }
        }
    }
}

