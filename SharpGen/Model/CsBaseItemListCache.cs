using System.Collections.Generic;
using System.Linq;

namespace SharpGen.Model
{
    internal interface IExpiring
    {
        void Expire();
    }

    internal struct CsBaseItemListCache<T> where T : CsBase
    {
        private ImmutableCacheList list;

        public IReadOnlyList<T> GetList(CsBase container)
        {
            if (list is {Invalid: true})
                list = null;

            return list ??= new ImmutableCacheList(container.Items.OfType<T>());
        }

        public IEnumerable<T> Enumerate(CsBase container)
        {
            if (list is {Invalid: true})
                list = null;

            return list ?? container.Items.OfType<T>();
        }

        public IExpiring Expiring => list;

        private sealed class ImmutableCacheList : List<T>, IExpiring
        {
            public bool Invalid;

            public ImmutableCacheList(IEnumerable<T> collection) : base(collection)
            {
            }

            public void Expire()
            {
                Invalid = true;
            }
        }
    }
}