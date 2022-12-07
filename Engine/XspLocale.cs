using System;
using System.Xml.Serialization;

namespace XSP.Engine
{
	public class XspLocale
	{
		public XspLocale(string language)
		{
			Language = language;
		}

		[XmlAttribute(AttributeName = "l")]
		public string Language
        {
			get; private set;
        }
	}
}

