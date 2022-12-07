using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSP.Engine.Schema.Expressions
{
    internal abstract class Node
    {
        public abstract object? Evaluate(object? source);

        public virtual object? Evaluate(IReadOnlyDictionary<string, Object> source)
        {
            return Evaluate((object)source);
        }

        public virtual object? Evaluate(IReadOnlyList<Object> source) 
        {
            return Evaluate((object)source);
        }

        public static T? Cast<T>(object? value) {
            if (value == null)
            {
                return default;
            }

            if(typeof(T).IsAssignableFrom(value.GetType())) {
                return (T)value;
            }

            if(typeof(T) == typeof(string)) {
                return (T?)(object?)value.ToString();
            }

            // need to provide converter logic ...
            throw new NotSupportedException();
        }
    }
}
