using System.Collections.Generic;

namespace VoronoiApp.Algo.DataStructures
{
    public class MultiValueDictionary<Key, Value> : Dictionary<Key, List<Value>>
    {
        private int _defaultListCapacity;



        /// <summary>
        /// Creates a new instance of <see cref="MultiValueDictionary{Key, Value}"/>.
        /// </summary>
        /// <param name="defaultListCapacity">Default capacity for value list.</param>
        public MultiValueDictionary(int defaultListCapacity = 0)
        {
            _defaultListCapacity = defaultListCapacity;
        }

        public void Add(Key key, Value value)
        {
            if (!TryGetValue(key, out List<Value> values))
            {
                values = new List<Value>(_defaultListCapacity);
                Add(key, values);
            }

            values.Add(value);
        }
    }
}
