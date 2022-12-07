using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSP.Engine.Schema.Expressions
{
    internal static class Expression
    {
        private static readonly LRUMap<string, Node> EXPRESSION_CACHE = new(100);

        public static bool HasExpression(String? expression)
        {
            return (expression != null && expression.Contains(ExpressionParser.TOKEN_START));
        }

        public static T? Evaluate<T>(String expression, IReadOnlyDictionary<String, Object> source)
        {
            return EvaluateInternal<T>(expression, source, node => node.Evaluate(source));
        }

        public static T? Evaluate<T>(String expression, IReadOnlyList<Object> source)
        {
            return EvaluateInternal<T>(expression, source, node => node.Evaluate(source));
        }

        public static T? Evaluate<T>(String expression, object source)
        {
            return EvaluateInternal<T>(expression, source, node => node.Evaluate(source));
        }

        private static T? EvaluateInternal<T>(String expression, object source, Func<Node, object?> processor)
        {
            try
            {
                if (expression == null)
                {
                    throw new ArgumentException("expression cannot be null");
                }

                if (source == null)
                {
                    throw new ArgumentException("source cannot be null");
                }

                Object? result;
                if (expression == null || !expression.Contains(ExpressionParser.TOKEN_START))
                {
                    result = expression;
                }
                else
                {
                    Node node = Parse(expression);
                    result = processor(node);
                }

                return Node.Cast<T>(result);
            }
            catch (Exception ex)
            {
                throw new ExpressionException(ex);
            }
        }

        public static Node Parse(String expression) {
            Node? node = EXPRESSION_CACHE.Get(expression);
            if(node == null) {
                node = ExpressionParser.Parse(expression);
                EXPRESSION_CACHE.Add(expression, node!);
            }

            return node!;
        }
    }
}
