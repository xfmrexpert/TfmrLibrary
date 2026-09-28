using System.Collections;

namespace TfmrLib.FEM
{
    public class NamedCollection<T> : IEnumerable<T> where T: INamed
    {
        private Dictionary<string, T> _collection = [];

        public void Add(T item)
        {
            _collection[item.Name] = item;
        }

        public T this[string name]
        {
            get {
                return _collection[name];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in _collection)
            {
                yield return item.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

    }
}