using System;
using System.ComponentModel;
#if !NETSTANDARD1_3
using System.Runtime.ConstrainedExecution;
#endif

namespace SharpGen.Runtime
{
    /// <summary>
    /// Base class for a <see cref="IDisposable"/> class.
    /// </summary>
    public abstract partial class DisposeBase :
#if !NETSTANDARD1_3
        CriticalFinalizerObject,
#endif
        IEnlightenedDisposable, IDisposable
    {
        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="DisposeBase"/> is reclaimed by garbage collection.
        /// </summary>
        ~DisposeBase() => CheckAndDispose(false);

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected abstract bool IsDisposed { get; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
#pragma warning disable CA1816
        public void Dispose() => CheckAndDispose(true);
#pragma warning restore CA1816

        /// <inheritdoc />
        void IEnlightenedDisposable.CheckAndDispose(bool disposing) => CheckAndDispose(disposing);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        private void CheckAndDispose(bool disposing)
        {
            if (IsDisposed)
                return;

            InvokeDisposeEventHandler(disposing, DisposingEventLock, DisposingEventTable);

            Dispose(disposing);

#pragma warning disable CA1816
            GC.SuppressFinalize(this);
#pragma warning restore CA1816

            InvokeDisposeEventHandler(disposing, DisposedEventLock, DisposedEventTable);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected abstract void Dispose(bool disposing);
    }
}