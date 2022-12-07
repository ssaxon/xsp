using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSP.Engine.Schema.Expressions
{
    internal class PrimitiveNode<T> : Node
    {
        private readonly T? value;

        public PrimitiveNode(T? value)
        {
            this.value = value;
        }

        public override object? Evaluate(object? source)
        {
            return value;
        }
    }
}