using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace XSP.Engine.Schema.Expressions
{
    internal class LRUMap<K, V> where K: notnull
    {
        private readonly int capacity;
        private readonly Dictionary<K, LinkedListNode<LRUMapEntry<K, V>>> cacheMap = new();
        private readonly LinkedList<LRUMapEntry<K, V>> lruList = new();

        public LRUMap(int capacity)
        {
            this.capacity = capacity;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public V? Get(K key)
        {
            if (cacheMap.TryGetValue(key, out LinkedListNode<LRUMapEntry<K, V>>? node))
            {
                //System.Console.WriteLine("Cache HIT " + key);
                V value = node.Value.value;

                lruList.Remove(node);
                lruList.AddLast(node);
                return value;
            }
            //System.Console.WriteLine("Cache MISS " + key);
            return default;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Add(K key, V val)
        {
            if (cacheMap.Count >= capacity)
            {
                RemoveFirst();
            }

            if (cacheMap.TryGetValue(key, out LinkedListNode<LRUMapEntry<K, V>>? node))
            {
                lruList.Remove(node);
                node.Value.value = val;
            }
            else
            {
                LRUMapEntry<K, V> cacheItem = new LRUMapEntry<K, V>(key, val);
                node = new LinkedListNode<LRUMapEntry<K, V>>(cacheItem);
            }

            lruList.AddLast(node);
            cacheMap.Add(key, node);
        }


        protected void RemoveFirst()
        {
            // Remove from LRUPriority
            LinkedListNode<LRUMapEntry<K, V>> node = lruList.First!;
            lruList.RemoveFirst();
            // Remove from cache
            cacheMap.Remove(node.Value.key);
        }
    }
}
