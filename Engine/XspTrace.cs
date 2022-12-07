using System;
using System.Xml;
using System.Xml.XPath;

namespace XSP.Engine
{
	public class XspTraceWriter
	{
		private XmlWriter? writer;
        private Stack<XspTraceScope> scopes = new();
        private HashSet<XspScope> writtenScopes = new();

		public XspTraceWriter(XmlWriter? xmlWriter)
		{
			this.writer = xmlWriter;
		}

		public void Begin()
		{
			if (this.writer != null)
            {
				this.writer.WriteStartDocument();
				this.writer.WriteStartElement("trace", "trace", XspNamespaces.TraceNamespaceURI);
            }
		}

		public void End()
        {
			if (this.writer != null)
			{
				this.writer.WriteEndElement();
				this.writer.WriteEndDocument();
				this.writer.Flush();
			}
		}

        public XspTraceScope Current => scopes.Count > 0 ? scopes.Peek() : null;

        public IXspTraceScope CreateScriptScope(string name, XspScript? script)
        {
            return new XspTraceScope(this, scopes, writer, "script", name, setup: (_, w) =>
            {
                if (script != null && !writtenScopes.Contains(script.FileScope))
                {
                    writtenScopes.Add(script.FileScope);
                    WriteScope(w, script.FileScope);
                }
            });
        }

        private void WriteScope(XmlWriter writer, XspScope scope)
        {
            foreach (var pair in scope.Local)
            {
                if (pair.Value is IXPathNavigable x)
                {
                    WriteXml(pair.Key, x);
                }
                else
                {
                    WriteAssign(pair.Key, pair.Value);
                }
            }
        }

        private void WriteAssign(string name, object? value)
        {
            if (writer != null)
            {
                writer.WriteStartElement("assign", XspNamespaces.TraceNamespaceURI);
                writer.WriteAttributeString("name", name);
                if (value != null)
                {
                    writer.WriteAttributeString("value", value.ToString());
                    writer.WriteAttributeString("type", value.GetType().Name);
                }
                writer.WriteEndElement();
            }
        }

        private void WriteXml(string name, IXPathNavigable value)
        {
            if (writer != null)
            {
                writer.WriteStartElement("xml", XspNamespaces.TraceNamespaceURI);
                writer.WriteAttributeString("name", name);
                value.CreateNavigator()!.WriteSubtree(writer);
                writer.WriteEndElement();
            }
        }

        public class XspTraceScope : IXspTraceScope
        {
            private readonly XspTraceWriter traceWriter;
            private readonly Stack<XspTraceScope> scopes;
            private readonly XmlWriter? writer;
            private readonly string scriptName;
            private bool disposedValue;

            public XspTraceScope(
                XspTraceWriter traceWriter,
                Stack<XspTraceScope> scopes,
                XmlWriter? writer,
                string type,
                string name,
                string? scriptName = null,
                Action<string, XmlWriter>? setup = null)
            {
                this.traceWriter = traceWriter;
                this.scopes = scopes;
                this.writer = writer;
                this.scriptName = scriptName ?? name;
                this.scopes.Push(this);

                if (writer != null)
                {
                    writer.WriteStartElement(type, XspNamespaces.TraceNamespaceURI);
                    writer.WriteAttributeString("href", name);
                    setup?.Invoke(this.scriptName, writer);
                }
            }

            public IXspTraceScope CreateScriptScope(string name, XspScript? script)
            {
                return traceWriter.CreateScriptScope(name, script);
            }

            public IXspTraceScope CreateSubScope(string name, XspScriptName scriptName)
            {
                return new XspTraceScope(traceWriter, scopes, writer, "call", name, scriptName.ShortName, (s, w) =>
                {
                    if (s != this.scriptName)
                    {
                        w.WriteAttributeString("script", s);
                    }
                });
            }

            public void WriteAssign(string name, object? value)
            {
                traceWriter.WriteAssign(name, value);
            }

            public void WriteError(XspError error)
            {
                if (writer != null)
                {
                    writer.WriteStartElement("error", XspNamespaces.TraceNamespaceURI);
                    writer.WriteAttributeString("message", error.Message);
                    if (error.Exception != null)
                    {
                        writer.WriteStartElement("exception", XspNamespaces.TraceNamespaceURI);
                        writer.WriteAttributeString("message", error.Exception.Message);
                        writer.WriteAttributeString("type", error.Exception.GetType().FullName);
                        writer.WriteStartElement("stackTrace", XspNamespaces.TraceNamespaceURI);
                        writer.WriteCData(error.Exception.StackTrace);
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
            }

            public void WriteXml(string name, IXPathNavigable value)
            {
                traceWriter.WriteXml(name, value);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        if (writer != null)
                        {
                            writer.WriteEndElement();
                        }
                        scopes.Pop();
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    disposedValue = true;
                }
            }

            // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
            // ~XspTraceScope()
            // {
            //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            //     Dispose(disposing: false);
            // }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }

	public interface IXspTraceScope : IDisposable
    {
        IXspTraceScope CreateScriptScope(string name, XspScript? script);
        IXspTraceScope CreateSubScope(string subName, XspScriptName scriptName);
        
        void WriteAssign(string variable, object? value);
        void WriteError(XspError error);
        void WriteXml(string variable, IXPathNavigable value);
	}
}

