using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SharpGen.Runtime.Win32
{
    partial class IPropertyStore : IReadOnlyDictionary<PropertyKey, Variant>
    {
        public bool TryGetValue(PropertyKey key, out Variant value)
        {
            value = this[key];
            return value.ElementType switch
            {
                VariantElementType.Empty => false,
                VariantElementType.Null => false,
                _ => true
            };
        }

        public Variant this[PropertyKey key]
        {
            get => GetValue(ref key);
            set => SetValue(ref key, value);
        }

        public IEnumerable<PropertyKey> Keys => Enumerable.Range(0, Count).Select(GetAt);
        public IEnumerable<Variant> Values => Keys.Select(x => this[x]);

        public bool ContainsKey(PropertyKey key) =>
            Keys.Any(x => x.PropertyId == key.PropertyId && x.FormatId == key.FormatId);

        public IEnumerator<KeyValuePair<PropertyKey, Variant>> GetEnumerator() =>
            Keys.Select(x => new KeyValuePair<PropertyKey, Variant>(x, this[x])).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}