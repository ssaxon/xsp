using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace XSP.Engine
{
    public class XspScope : IReadOnlyDictionary<string, Object>
	{
        private readonly string scopeName;
        private readonly XspScope? parent;
        private readonly HashSet<string> keys = new();
        private readonly Dictionary<string, object> values = new();

        private long snapshotId = 0;

		public XspScope(string scopeName, XspContext context, XspScope? parent = null, XspRef? xspRef = null, XspScope? evaluationScope = null)
        {
            this.scopeName = scopeName;
            this.Context = context;
            this.parent = parent;

            if (parent != null)
            {
                foreach(string k in parent.keys)
                {
                    keys.Add(k);
                }
            }

            if(xspRef != null)
            {
                xspRef.CopyInto(this, evaluationScope!);
            }
        }

        public IReadOnlyDictionary<string, object> Local => values;

        internal XspContext Context
        {
            get; private set;
        }

        internal XspScopeSnapshot Snapshot()
        {
            return new XspScopeSnapshot(() => snapshotId);
        }

        public void Set(string key, object? value)
        {
            keys.Add(key);
#pragma warning disable CS8601 // Possible null reference assignment.
            values[key] = value;
#pragma warning restore CS8601 // Possible null reference assignment.

            Console.WriteLine($"scope: {scopeName}, key: {key}, value: {value}");
        }

        public object this[string key]
        {
            get
            {
                if (!TryGetValue(key, out object? value))
                {
                    throw new KeyNotFoundException();
                }

#pragma warning disable CS8603 // Possible null reference return.
                return value;
#pragma warning restore CS8603 // Possible null reference return.
            }
        }

        public bool ContainsKey(string key)
        {
            if (key == "query" || key == "locale")
            {
                return true;
            }

            return keys.Contains(key);
        }

        IEnumerable<string> IReadOnlyDictionary<string, Object>.Keys => keys;

        IEnumerable<object> IReadOnlyDictionary<string, Object>.Values
        {
            get
            {
                IReadOnlyDictionary<string, Object> d = this;

                foreach(string key in keys)
                {
                    yield return d[key];
                }
            }
        }

        int IReadOnlyCollection<KeyValuePair<string, Object>>.Count => keys.Count;

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, Object>>.GetEnumerator()
        {
            IReadOnlyDictionary<string, Object> d = this;

            foreach (string key in keys)
            {
                yield return new KeyValuePair<string, object>(key, d[key]);
            }
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
        {
            snapshotId++;

            if (key == "query")
            {
                value = Context.QueryArguments;
                return true;
            }

            if (key == "locale")
            {
                value = Context.Locale;
                return true;
            }

            if (values.TryGetValue(key, out value))
            {
                return true;
            }

            return parent?.TryGetValue(key, out value) ?? false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            IEnumerable<KeyValuePair<string, Object>> e = this;
            return e.GetEnumerator();
        }
    }
}

