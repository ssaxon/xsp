using System;
using System.Xml;

namespace XSP.Engine.Schema
{
	public class XspSource
	{
		public readonly XspScriptName ScriptName;
		public readonly int LineNumber;

		internal XspSource(XmlReader reader, XspParser parser)
			: this(parser.Script.ScriptName, reader.LineNumber())
        {

        }

		internal XspSource(XspScriptName scriptName, int lineNumber)
		{
			ScriptName = scriptName;
			LineNumber = lineNumber;
		}
	}
}

