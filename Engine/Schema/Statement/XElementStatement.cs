using System;
using System.Xml;
using XSP.Engine;

namespace XSP.Engine.Schema.Statement
{
    public class XElementStatement: XspStatement
	{
		public readonly XName Name;
		public readonly IEnumerable<XspStatement>? Statements;
		private readonly Dictionary<string, XAttribute>? attributes;

		internal XElementStatement(XmlReader reader, XspParser parser)
			: base(new XspSource(reader, parser))
		{
			Name = new XName(reader);

			if (reader.HasAttributes)
            {
				attributes = new();

				for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
				{
					reader.MoveToAttribute(attInd);

					var a = new XAttribute(reader);
					attributes.Add(a.Name, a);

				}
				reader.MoveToElement();
			}

			Statements = parser.ParseStatements();
		}

		public override XspError? Execute(XspScript script, XspScope scope, XmlWriter? writer)
		{
			Name.WriteTo(writer!, scope);

			if (attributes != null)
            {
				foreach(var attr in attributes.Values)
                {
					attr.WriteTo(writer!, scope);
				}
			}

			var error = Statements.Execute(script, scope, writer);

			writer!.WriteEndElement();
			
			return error;
		}
	}
}

