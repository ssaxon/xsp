using System;
using System.Xml.XPath;

namespace System.Xml
{
	public static class XmlExtensions
	{
		public static int LineNumber(this XmlReader reader)
		{
			var xmlInfo = (IXmlLineInfo)reader;
			return xmlInfo.LineNumber;
		}

		public static XmlWriter CreateWriter(this IXPathNavigable doc)
		{
			return doc.CreateNavigator()!.AppendChild();
		}

		public static void WriteRoot(this IXPathNavigable navigable, XmlWriter writer)
        {
			var nav = navigable.CreateNavigator()!;
			nav.MoveToChild(XPathNodeType.Element);
			nav.WriteSubtree(writer);
		}

		public static string ToIsoDate(this DateTime dateTime)
        {
			return dateTime.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz");
		}
	}
}

