using System.Collections.Generic;

namespace SharpGen.Config
{
    public partial class ConfigFile
    {
        private sealed class IdEqualityComparer : IEqualityComparer<ConfigFile>
        {
            public bool Equals(ConfigFile x, ConfigFile y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Id == y.Id;
            }

            public int GetHashCode(ConfigFile obj)
            {
                return obj.Id != null ? obj.Id.GetHashCode() : 0;
            }
        }

        public static IEqualityComparer<ConfigFile> IdComparer { get; } = new IdEqualityComparer();
    }
}