using System;
using System.Xml;
using XSP.Engine.Schema.Expressions;

namespace XSP.Engine.Schema.Statement
{
	public class XspLiteralStatement: XspStatement
	{
		private readonly NodeOrString nodeOrString;

		internal XspLiteralStatement(XspSource source, string text)
			: base(source)
		{
			this.nodeOrString = new NodeOrString(text);
		}

        public override XspError? Execute(XspScript script, XspScope scope, XmlWriter? writer)
        {
			writer!.WriteString(this.nodeOrString.GetValue(scope));
			return null;
        }
    }
}

