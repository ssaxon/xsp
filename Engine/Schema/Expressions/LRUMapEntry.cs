using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSP.Engine.Schema.Expressions
{
    internal class LRUMapEntry<K, V>
    {
        public LRUMapEntry(K k, V v)
        {
            key = k;
            value = v;
        }
        public K key;
        public V value;
    }
}
