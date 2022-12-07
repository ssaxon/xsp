using System.Xml;
using XSP.Engine.Schema.Expressions;

namespace XSP.Engine.Schema.Statement
{
    public class XAttribute : XName
    {
		private readonly NodeOrString nodeOrString;

		public XAttribute(XmlReader reader): base(reader)
        {
			nodeOrString = new NodeOrString(reader.Value.Length > 0 ? reader.Value : null);
		}

		public override void WriteTo(XmlWriter writer, XspScope scope)
        {
			writer.WriteAttributeString(LocalName, NamespaceURI, nodeOrString.GetValue(scope));
        }
	}
}

