namespace XSP.Engine.Schema.Expressions
{
    internal struct NodeOrString
    {
        private readonly string? text;
        private readonly Node? node;

        public static implicit operator NodeOrString(string text)
        {
            return new NodeOrString(text);
        }

        public NodeOrString(string? text)
        {
            if (Expression.HasExpression(text))
            {
                this.node = Expression.Parse(text!);
                this.text = null;
            }
            else
            {
                this.node = null;
                this.text = text;
            }
        }

        public string? GetValue(XspScope scope)
        {
            return text ?? Node.Cast<string>(node!.Evaluate(scope));
        }
    }
}
