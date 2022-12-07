using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSP.Engine.Schema.Expressions
{
    internal abstract class BinaryNode : Node
    {
        protected readonly Node Left;
        protected readonly Node Right;

        public BinaryNode(Node left, Node right)
        {
            this.Left = left;
            this.Right = right;
        }
    }
}
