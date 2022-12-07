using System.Collections.Immutable;
using System.Reflection;
using System.Xml.Serialization;

namespace XSP.Engine.Schema.Expressions
{
    internal class Property : Node
    {
        private readonly String name;
        private bool nullable;
        private Property? child;
        private int? ordinal;
        private static readonly LRUMap<Type, Dictionary<String, PropertyInfo>> PROPERTY_CACHE = new(100);

        private Property(String name, bool nullable, int index)
        {
            if (name[0] != '_' && !Char.IsLetter(name[0]))
            {
                throw new ParseException("Invalid property name", index);
            }

            this.name = name;
            this.nullable = nullable;
        }

        public bool Nullable
        {
            set { this.nullable = value; }
        }

        public int Ordinal
        {
            set { this.ordinal = value; }
        }

        public static Property Append(Property? source, String name, bool nullable, int index)
        {
            Property mine = new(name, nullable, index);
            if (source != null)
            {
                source.child = mine;
            }

            return mine;
        }

        public override object? Evaluate(IReadOnlyDictionary<String, Object> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (name == null)
            {
                throw new ArgumentException("Expected a property name");
            }

            if (!source.ContainsKey(name) && !nullable)
            {
                throw new ArgumentException(String.Format("No key named '{0}'", name));
            }

            source.TryGetValue(name, out object? value);
            return Process(value);
        }

        public override object? Evaluate(IReadOnlyList<Object> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if ("length".Equals(name))
            {
                if (child != null || nullable)
                {
                    throw new ArgumentException("Unexpected character after 'length'");
                }

                return source.Count;
            }

            if (name != null)
            {
                throw new ArgumentException("Cannot not get " + name + " from a list");
            }

            if (!ordinal.HasValue)
            {
                throw new ArgumentException("Expected an array index");
            }

            if (this.nullable && ordinal >= source.Count)
            {
                return null;
            }

            return Process(source[ordinal.Value]);
        }

        public override object? Evaluate(object? source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (name == null)
            {
                throw new ArgumentException("Expected a property name");
            }

            if (source is IReadOnlyList<Object> list)
            {
                return Evaluate(list);
            }

            if (source is IReadOnlyDictionary<String, Object> dict)
            {
                return Evaluate(dict);
            }

            Dictionary<String, PropertyInfo> methodMap = GetProperties(source.GetType());
            if (!methodMap.ContainsKey(name))
            {
                throw new ArgumentException(String.Format("No property named '{0}'", name));
            }

            object? result = methodMap[name].GetValue(source);
            return Process(result);
        }

        private object? Process(object? result)
        {
            if (child != null)
            {
                if (!this.nullable || result != null)
                {
                    if (result is IReadOnlyList<object>)
                    {
                        return child.Evaluate((IReadOnlyList<Object>)result);
                    }

                    if (result is IReadOnlyDictionary<string, object>)
                    {
                        return child.Evaluate((IReadOnlyDictionary<String, Object>)result);
                    }

                    return child.Evaluate(result);
                }
            }

            return result;
        }

        private Dictionary<String, PropertyInfo> GetProperties(Type type)
        {
            Dictionary<String, PropertyInfo>? methodMap = PROPERTY_CACHE.Get(type);
            if (methodMap != null)
            {
                return methodMap;
            }

            methodMap = new();

            foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!pi.CanRead)
                {
                    continue;
                }

                string name = pi.Name;
                XmlAttributeAttribute? attr = pi.GetCustomAttribute<XmlAttributeAttribute>();
                if (attr != null)
                {
                    name = attr.AttributeName;
                }
                else
                {
                    name = Char.ToLower(name[0]).ToString();

                    if (pi.Name.Length > 1)
                    {
                        name += pi.Name[1..];
                    }
                }

                methodMap.Add(name, pi);
            }

            PROPERTY_CACHE.Add(type, methodMap);

            return methodMap;
        }
    }
}