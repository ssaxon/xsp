using System;
using System.Xml;

namespace XSP.Engine
{
	public class XspException: Exception
	{
		public XspError Error { get; private set; }

		public XspException(XspError error) : base(error.Message)
		{
			Error = error;
		}

		public XspException(string error, XspScript script, XmlReader reader)
			: this(new XspError(error, script, reader))
		{
		}
	}
}

