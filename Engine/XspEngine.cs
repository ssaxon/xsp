using System;
using System.Collections.Concurrent;
using System.Runtime.Caching;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using XSP.Engine.Schema.Statement;

namespace XSP.Engine
{
	public class XspEngine
	{
		private readonly XspFileCache fileCache = new();
		private readonly XspFileCache scriptCache = new();
		private const int fileCacheSeconds = 60 * 2;
		private const int fileSetCacheSeconds = 60 * 10;
		private const int scriptCacheSeconds = 60 * 10;
		private static readonly XmlDocument emptyDocument = new();

		internal XspResolver Resolver { get; private set; }

		internal XmlReaderSettings ReaderSettings { get; } = new XmlReaderSettings()
		{
			CloseInput = true,
			IgnoreComments = true,
			IgnoreWhitespace = true,
			NameTable = new XmlDocument().NameTable,			
		};

		public XspEngine(): this("./xsproot")
		{
			scriptCache.FileChangedHandler = filename =>
			{
				XspXmlStatement.ClearCache();
			};
		}

		public XspEngine(string scriptRoot)
		{
			Resolver = new XspResolver(scriptRoot);
		}

		public XspResult<XmlDocument> Execute(string script, XspContext context)
		{
			XmlDocument target = new();

			return new XspResult<XmlDocument>(target, Execute(script, context, target));
		}

		public XspError? Execute(string script, XspContext context, XmlDocument target)
        {
			var startTime = DateTime.Now;
			var scriptResult = context.GetScript(script);

			using var traceScope = context.TraceWriter.CreateScriptScope(script, scriptResult.Value);			

			if (scriptResult.Error != null)
            {
				traceScope.WriteError(scriptResult.Error);
				return scriptResult.Error;
			}

			try
			{
				using XmlWriter writer = target.CreateWriter();
				writer.WriteStartDocument();
				writer.WriteStartElement("doc", "root", XspNamespaces.DocNamespaceURI);

				try
				{
					var xspRef = XspRef.From(script, Resolver, scriptResult.Value!);
					return scriptResult.Value!.Execute(xspRef, context.CurrentScope, writer);
				}
				catch (XspException ex)
				{
					return ex.Error;
				}
				catch (Exception ex)
				{
					return new XspError(ex);
				}
				finally
				{
					writer.WriteEndElement();
					writer.WriteEndDocument();
					writer.Flush();
				}
			}
			finally
            {
				target.DocumentElement!.SetAttribute("executionMs", (DateTime.Now - startTime).TotalMilliseconds.ToString("0.000"));
			}
		}

		internal XspResult<XspScript> LoadScript(string script, XspContext context, string? pathContext = default)
        {
			string path = Resolver.Resolve(script, pathContext);

			return this.scriptCache.GetOrAdd<XspScript>(path, path => XspScript.Load(path, this, context), scriptCacheSeconds);
		}

		internal XspResult<IXPathNavigable> LoadXml(string xmlFileName, string? pathContext = default)
		{
			return LoadXml(xmlFileName, out _, pathContext);
		}

		internal XspResult<IXPathNavigable> LoadXml(string xmlFileName, out IEnumerable<string> sources, string? pathContext = default)
		{
			string path = Resolver.Resolve(xmlFileName, pathContext);

			if (!path.Contains('*') && !path.Contains('?'))
			{
				return fileCache.GetOrAdd<IXPathNavigable>(path, path => new XPathDocument(path), out sources);
			}

			var directory = Path.GetDirectoryName(path)!;
			if (!Directory.Exists(directory))
			{
				sources = Array.Empty<string>();
				return emptyDocument;
			}

			return fileCache.GetOrAdd<IXPathNavigable>(path, path =>
			{
				var filePart = Path.GetFileName(path)!;
				var xmlDoc = new XmlDocument(this.ReaderSettings.NameTable!);

				List<string> filePaths = new() { directory! };

				using (var writer = xmlDoc.CreateWriter())
				{
					writer.WriteStartDocument();
					writer.WriteStartElement("doc", "files", XspNamespaces.DocNamespaceURI);

					var fileNames = Directory.GetFiles(directory, filePart);
					foreach (var fileName in fileNames)
					{
						filePaths.Add(fileName);

						var xml = LoadXmlFile(fileName);
						if (xml.Error != null)
						{
							throw new XspException(xml.Error);
						}

						var nav = xml.Value!.CreateNavigator()!;
						if (!nav.MoveToChild(XPathNodeType.Element))
						{
							continue;
						}

						writer.WriteStartElement("doc", "file", XspNamespaces.DocNamespaceURI);
						writer.WriteAttributeString("name", Path.GetFileNameWithoutExtension(fileName));
						writer.WriteAttributeString("path", fileName);
						writer.WriteAttributeString("lastModified", File.GetLastWriteTime(fileName).ToIsoDate());

						nav.WriteSubtree(writer);
						writer.WriteEndElement();
					}

					writer.WriteEndElement();
					writer.WriteEndDocument();
				}

				return new Tuple<IXPathNavigable, List<string>>(xmlDoc, filePaths);
			}, out sources, fileSetCacheSeconds);
		}

		internal XspResult<IXPathNavigable> LoadXmlFile(string path)
		{
			return fileCache.GetOrAdd<IXPathNavigable>(path, path => new XPathDocument(path), fileCacheSeconds);
		}

		internal XspResult<XslCompiledTransform> LoadXsltFile(string path, int seconds)
		{
			var fullPath = Resolver.Resolve(path, null);

			return fileCache.GetOrAdd<XslCompiledTransform>(fullPath, fullPath =>
			{
				var xslTransform = new XslCompiledTransform();
				xslTransform.Load(fullPath);
				return xslTransform;
			});
		}
	}
}

