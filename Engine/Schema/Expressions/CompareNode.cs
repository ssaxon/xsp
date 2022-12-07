using System;
namespace XSP.Engine.Schema.Expressions
{
	internal class CompareNode: BinaryNode
	{
        private readonly ComparisonOperator oper;

        public CompareNode(Node left, Node right, ComparisonOperator oper)
			: base(left, right)
		{
            this.oper = oper;
		}

        public override object? Evaluate(object? source)
        {
            var left = Left.Evaluate(source);
            var right = Right.Evaluate(source);
            int? comparable = null;

            if (left is IComparable lc)
            {
                comparable = lc.CompareTo(right);
            }
            else if (right is IComparable rc)
            {
                comparable = rc.CompareTo(right);
            }

            switch (oper)
            {
                case ComparisonOperator.EQ:
                    if (comparable.HasValue)
                    {
                        return comparable.Value == 0;
                    }
                    return Object.Equals(left, right);

                case ComparisonOperator.NE:
                    if (comparable.HasValue)
                    {
                        return comparable.Value != 0;
                    }
                    return !Object.Equals(left, right);
            }

            return null;
        }
    }

    public enum ComparisonOperator
    {
        EQ,
        NE
    }
}

