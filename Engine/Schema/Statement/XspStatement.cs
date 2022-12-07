using System;
using System.Xml;

namespace XSP.Engine.Schema.Statement
{
	public abstract class XspStatement
	{
		public readonly XspSource Source;

		protected XspStatement(XspSource source)
		{
			Source = source;
		}

		public abstract XspError? Execute(XspScript script, XspScope scope, XmlWriter? writer);
	}
}

