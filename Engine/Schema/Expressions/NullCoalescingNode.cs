using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSP.Engine.Schema.Expressions
{
    internal class NullCoalescingNode : BinaryNode
    {
        public NullCoalescingNode(Node left, Node right)
            : base(left, right)
        {
        }

        public override object? Evaluate(IReadOnlyDictionary<string, Object> source)
        {
            var result = Left.Evaluate(source);
            if (result == null)
            {
                result = Right.Evaluate(source);
            }

            return result;
        }

        public override object? Evaluate(IReadOnlyList<Object> source)
        {
            object? result = Left.Evaluate(source);
            if (result == null)
            {
                result = Right.Evaluate(source);
            }

            return result;
        }

        public override object? Evaluate(object? source)
        {
            var result = Left.Evaluate(source);
            if (result == null)
            {
                result = Right.Evaluate(source);
            }

            return result;
        }
    }
}
