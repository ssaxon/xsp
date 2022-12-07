using System;
using System.Xml;
using XSP.Engine.Schema.Expressions;

namespace XSP.Engine.Schema.Statement
{
	public class XspIfStatement : XspStatement
	{
		private readonly Node condition;
		public readonly IEnumerable<XspStatement>? Statements;

		internal XspIfStatement(XmlReader reader, XspParser parser, XspResolver resolver)
			: base(new XspSource(reader, parser))
		{
			var test = reader.GetAttribute("test");
			if (test == null)
            {
				parser.Raise("xsp:if missing test=");
            }

			condition = Expression.Parse(test!);

			reader.MoveToElement();
			Statements = parser.ParseStatements();
		}

        public override XspError? Execute(XspScript script, XspScope scope, XmlWriter? writer)
        {
			if (Node.Cast<bool>(condition.Evaluate(scope)))
			{
				return Statements.Execute(script, scope, writer);
            }

			return null;
        }
    }
}

