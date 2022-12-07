using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XSP.Engine.Schema.Expressions
{
    internal class ContainerNode : Node
    {
        private List<Node> nodes = new List<Node>();

        public void Add(Node node)
        {
            nodes.Add(node);
        }

        public Node FirstOrSelf()
        {
            return this.nodes.Count == 1 ? this.nodes[0] : this;
        }

        public override object? Evaluate(object? source)
        {
            StringBuilder buffer = new();

            foreach (Node node in nodes)
            {
                buffer.Append(Node.Cast<string>(node.Evaluate(source)));
            }

            return buffer.ToString();
        }
    }
}
