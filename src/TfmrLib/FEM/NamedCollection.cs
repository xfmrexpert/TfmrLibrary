using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TfmrLib.FEM
{
    /// <summary>
    /// A list of named items that also supports O(1) lookup by <see cref="INamed.Name"/>.
    /// <para>
    /// Backed by <see cref="KeyedCollection{TKey,TItem}"/>: the key is embedded in the item
    /// (its <c>Name</c>), so there is no separate dictionary to keep in sync, ordering and
    /// index access are preserved, and adding a duplicate name throws. Names are compared
    /// case-insensitively.
    /// </para>
    /// </summary>
    /// <typeparam name="T">An item type whose <c>Name</c> is its unique key.</typeparam>
    public class NamedCollection<T> : KeyedCollection<string, T> where T : INamed
    {
        public NamedCollection()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public NamedCollection(IEnumerable<T> items)
            : this()
        {
            ArgumentNullException.ThrowIfNull(items);
            foreach (var item in items)
            {
                Add(item);
            }
        }

        protected override string GetKeyForItem(T item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return item.Name;
        }

        /// <summary>
        /// Re-keying an item would desynchronize it from the embedded <see cref="INamed.Name"/>.
        /// Names are immutable, so this is blocked to surface programming errors early.
        /// </summary>
        protected override void SetItem(int index, T item)
        {
            ArgumentNullException.ThrowIfNull(item);
            base.SetItem(index, item);
        }

        /// <summary>Re-keying is not supported; <see cref="INamed.Name"/> is immutable.</summary>
        protected new void ChangeItemKey(T item, string newKey)
            => throw new NotSupportedException(
                $"{typeof(T).Name} names are immutable once added to a {nameof(NamedCollection<T>)}.");
    }
}
