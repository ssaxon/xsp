using System;
using System.Xml;
using XSP.Engine.Schema.Expressions;

namespace XSP.Engine.Schema.Statement
{
	public class XspAssignStatement: XspStatement
	{
		private readonly string? name;
		private readonly Node node;

		internal XspAssignStatement(XmlReader reader, XspParser parser, XspResolver resolver)
			: base(new XspSource(reader, parser))
		{
			name = reader.GetAttribute("name");
			if (name == null)
			{
				parser.Raise("xsp:assign missing name=");
			}

			var value = reader.GetAttribute("value");
			if (value == null)
			{
				parser.Raise("xsp:assign missing value=");
			}

			node = Expression.Parse(value!);
		}

		public override XspError? Execute(XspScript script, XspScope scope, XmlWriter? writer)
        {
			object? value = node.Evaluate(scope);

			scope.Context.TraceWriter.Current?.WriteAssign(name!, value);

			scope.Set(name!, value);
			return null;
        }
    }
}

