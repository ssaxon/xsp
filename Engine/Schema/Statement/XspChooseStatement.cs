using System;
using System.Xml;
using XSP.Engine.Schema.Expressions;

namespace XSP.Engine.Schema.Statement
{
	public class XspChooseStatement : XspStatement
	{
		private readonly List<Tuple<Node, IEnumerable<XspStatement>?>> when = new();
		private IEnumerable<XspStatement>? otherwiseStatements;
		private bool hasOtherwise = false;

		internal XspChooseStatement(XmlReader reader, XspParser parser, XspResolver resolver)
			: base(new XspSource(reader, parser))
		{
			reader.MoveToContent();

			parser.ParseElements(_ =>
			{
				if (reader.NamespaceURI == parser.XspNamespace)
				{
					if (reader.LocalName == "when")
					{
						if (hasOtherwise)
						{
							throw new XspException("Unexpected <xsp:when>", parser.Script, reader);
						}

						var test = reader.GetAttribute("test");
						if (test == null)
						{
							parser.Raise("xsp:when missing test=");
						}

						var condition = Expression.Parse(test!);

						reader.MoveToElement();
						var statements = parser.ParseStatements();

						when.Add(new Tuple<Node, IEnumerable<XspStatement>?>(condition, statements));
						return;
					}

					if (reader.LocalName == "otherwise")
					{
						if (reader.HasAttributes)
						{
							throw new XspException("Unexpected attribute on <xsp:otherwise>", parser.Script, reader);
						}

						reader.MoveToElement();
						this.otherwiseStatements = parser.ParseStatements();
						hasOtherwise = true;
						return;
					}
				}

				throw new XspException($"Unexpected element <{reader.Name}", parser.Script, reader);
			}, false, false);			
		}

		public override XspError? Execute(XspScript script, XspScope scope, XmlWriter? writer)
		{
			foreach (var tuple in this.when)
			{
				if (Node.Cast<bool>(tuple.Item1.Evaluate(scope)))
				{
					return tuple.Item2.Execute(script, scope, writer);
				}
			}

			return otherwiseStatements.Execute(script, scope, writer);
        }
    }
}

