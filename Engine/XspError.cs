using System.Xml;
using XSP.Engine.Schema;

namespace XSP.Engine
{
    public class XspError
	{
		public string Message { get; private set; }
		public Exception? Exception { get; private set; }

		public XspError(Exception source)
		{
			this.Message = source.Message;
			this.Exception = source;
		}

		public XspError(string message, XspSource? xspSource = null)
		{
			if (xspSource == null)
			{
				this.Message = message;
			}
			else
			{
				this.Message = $"{message} in {xspSource.ScriptName.ShortName} at line {xspSource.LineNumber}";
			}
		}

		internal XspError(string error, XspScript script, XmlReader reader)
		{
			this.Message = $"{error} in {script.ScriptName.ShortName} at line {reader.LineNumber()}";
		}
	}
}

