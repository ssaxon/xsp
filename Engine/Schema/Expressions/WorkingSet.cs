using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSP.Engine.Schema.Expressions
{
    internal class WorkingSet
    {
        private StringBuilder? name = null;
        private Node? result;
        private Property? property;
        private bool allowName = true;

        public bool NameAllowed
        {
            get { return allowName; }
            set { allowName = value; }
        }

        public bool NameIsEmpty
        {
            get { return (name == null || name.Length == 0); }
        }

        public string? GetName()
        {
            if (NameIsEmpty)
            {
                return null;
            }

            string value = name!.ToString();
            name.Length = 0;
            return value;
        }

        public void AppendName(char ch)
        {
            if (name == null)
            {
                name = new StringBuilder();
            }

            name.Append(ch);
        }

        public bool IsEmpty
        {
            get { return result == null; }
        }

        public bool AllowExpression
        {
            get { return IsEmpty && NameIsEmpty && allowName; }
        }

        public void DemandExpression(char ch, int ix)
        {
            if (!AllowExpression)
            {
                // unexpected parentheses ...
                throw new ParseException(string.Format(ExpressionParser.UNEXPECTED_CHAR, ch), ix);
            }
        }

        public Node GetResult(int index)
        {
            if (!NameIsEmpty)
            {
                Update(false, index);
            }

            if (result == null)
            {
                throw new ParseException("No expression defined", index);
            }

            return result;
        }

        public Property GetProperty(int index)
        {
            if (property == null)
            {
                throw new ParseException("No expression defined", index);
            }

            return property;
        }

        public void SetPrimitive<T>(T value)
        {
            SetResult(new PrimitiveNode<T>(value));
        }

        public void SetResult(Node value)
        {
            if (result != null)
            {
                throw new InvalidOperationException();
            }

            result = value;
        }

        public void Update(bool nullable, int index)
        {
            Update(GetName()!, nullable, index);
        }

        public void Update(string name, bool nullable, int index)
        {
            if ("null".Equals(name))
            {
                if (result != null)
                {
                    throw new ParseException("null cannot be used here", index);
                }

                result = new PrimitiveNode<object>(null);
                return;
            }

            if ("false".Equals(name) || "true".Equals(name))
            {
                if (result != null)
                {
                    throw new ParseException("true/false cannot be used here", index);
                }

                result = new PrimitiveNode<bool>(bool.Parse(name));
                return;
            }

            property = Property.Append(property, name, nullable, index);
            if (result == null)
            {
                result = property;
            }
        }
    }
}