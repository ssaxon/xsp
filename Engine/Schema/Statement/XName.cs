using System.Xml;

namespace XSP.Engine.Schema.Statement
{
    public class XName
	{
		public readonly string? Prefix;
		public readonly string LocalName;
		public readonly string Name;
		public readonly string? NamespaceURI;

		public XName(XmlReader reader)
        {
			Prefix = reader.Prefix.Length > 0 ? reader.Prefix : null;
			LocalName = reader.LocalName;
			Name = reader.Name;
			NamespaceURI = reader.NamespaceURI.Length > 0 ? reader.NamespaceURI : null;
		}

		public virtual void WriteTo(XmlWriter writer, XspScope scope)
        {
			writer.WriteStartElement(Prefix, LocalName, NamespaceURI);
        }
	}
}

