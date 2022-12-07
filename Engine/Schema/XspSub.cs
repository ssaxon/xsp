using System;
using XSP.Engine.Schema.Statement;

namespace XSP.Engine.Schema
{
	public class XspSub
	{
		public string Name { get; private set; }
		public IEnumerable<XspStatement> Statements { get; private set; }

		internal XspScope? FileScope { get; set; }
		internal XspScript? Script { get; set; }

		public XspSub(string name, IEnumerable<XspStatement> statements)
		{
			Name = name;
			Statements = statements;
		}
	}
}

