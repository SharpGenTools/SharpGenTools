using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SharpGen.Runtime
{
    using EventTable =  ConditionalWeakTable<DisposeBase, DisposeEventHandler>;

    /// <summary>
    /// Base class for a <see cref="IDisposable"/> class.
    /// </summary>
    public abstract partial class DisposeBase
    {
        private static readonly EventTable DisposingEventTable = new();
        private static readonly EventTable DisposedEventTable = new();
        private static readonly ReaderWriterLockSlim DisposingEventLock = new(LockRecursionPolicy.NoRecursion);
        private static readonly ReaderWriterLockSlim DisposedEventLock = new(LockRecursionPolicy.NoRecursion);

        [MethodImpl(Utilities.MethodAggressiveOptimization)]
        private void UpdateEventHandler(EventTable table, DisposeEventHandler value)
        {
#if NETFRAMEWORK || NETSTANDARD1_3 || NETSTANDARD2_0
            table.Remove(this);
            table.Add(this, value);
#else
            table.AddOrUpdate(this, value);
#endif
        }

        private void AddEventHandler(EventTable table, DisposeEventHandler value, ReaderWriterLockSlim rwLock)
        {
            rwLock.EnterWriteLock();
            try
            {
                if (table.TryGetValue(this, out var handler))
                    UpdateEventHandler(table, handler + value);
                else
                    table.Add(this, value);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        private void RemoveEventHandler(EventTable table, DisposeEventHandler value, ReaderWriterLockSlim rwLock)
        {
            rwLock.EnterUpgradeableReadLock();
            try
            {
                if (!table.TryGetValue(this, out var handler))
                    return;

                rwLock.EnterWriteLock();
                try
                {
                    UpdateEventHandler(table, handler - value);
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }
            }
            finally
            {
                rwLock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Occurs when this instance is starting to be disposed.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public event DisposeEventHandler Disposing
        {
            add => AddEventHandler(DisposingEventTable, value, DisposingEventLock);
            remove => RemoveEventHandler(DisposingEventTable, value, DisposingEventLock);
        }

        /// <summary>
        /// Occurs when this instance is fully disposed.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public event DisposeEventHandler Disposed
        {
            add => AddEventHandler(DisposedEventTable, value, DisposedEventLock);
            remove => RemoveEventHandler(DisposedEventTable, value, DisposedEventLock);
        }

        private void InvokeDisposeEventHandler(bool disposing, ReaderWriterLockSlim rwLock, EventTable table)
        {
            DisposeEventHandler value;

            rwLock.EnterReadLock();
            try
            {
                table.TryGetValue(this, out value);
            }
            finally
            {
                rwLock.ExitReadLock();
            }

            value?.Invoke(this, disposing);
        }
    }
}