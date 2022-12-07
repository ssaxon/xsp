using System;
using System.Xml;
using XSP.Engine.Schema.Statement;

namespace XSP.Engine
{
	internal static class XspStatementExtensions
	{
		public static XspError? Execute(this IEnumerable<XspStatement>? statements, XspScript script, XspScope scope, XmlWriter? writer)
		{
			if (statements == null)
            {
				return null;
            }

			foreach (var statement in statements)
			{
				var error = statement.Execute(script, scope, writer);
				if (error != null)
				{
					return error;
				}
			}

			return null;
		}
	}
}

