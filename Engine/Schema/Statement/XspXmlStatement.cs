using System;
using System.Runtime.Caching;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using XSP.Engine.Schema.Expressions;

namespace XSP.Engine.Schema.Statement
{
	public class XspXmlStatement : XspStatement
	{
		private readonly NodeOrString? cacheKey;
		private readonly TimeSpan? cacheDuration;
		private readonly string? name;
		private readonly string? src;
		private readonly IEnumerable<XspStatement>? Statements;

		private static readonly XspFileCache cache = new("xml");

		internal XspXmlStatement(XmlReader reader, XspParser parser, XspResolver resolver)
			: base(new XspSource(reader, parser))
		{
			name = reader.GetAttribute("name");
			if (name == null)
			{
				parser.Raise("xsp:xml missing name=");
			}

			var src = reader.GetAttribute("src");
			if (src != null)
            {
				if (!reader.IsEmptyElement)
                {
					parser.Raise("xsp:xml with src= cannot also have content");
				}

				this.src = resolver.ResolveAndSimplify(src!, parser.Script.ScriptName.Path);
			}

			var cacheKey = reader.GetAttribute("cacheKey");
			if (cacheKey != null)
			{
				this.cacheKey = new NodeOrString(cacheKey);

				var cacheDuration = reader.GetAttribute("cacheDuration");
				if (cacheDuration != null)
				{
					this.cacheDuration = TimeSpan.Parse(cacheDuration);
				}
				else
				{
					this.cacheDuration = TimeSpan.FromSeconds(60);
				}
			}

			reader.MoveToElement();
			Statements = parser.ParseStatements();
		}

		public static void ClearCache()
        {
			cache.Clear();
        }

		public override XspError? Execute(XspScript script, XspScope scope, XmlWriter? writer)
		{
			if (this.src != null)
			{
				var loaded = scope.Context.Engine.LoadXml(this.src, out IEnumerable<string> sources);
				if (loaded.Error != null)
				{
					scope.Context.TraceWriter.Current?.WriteError(loaded.Error);
					return loaded.Error;
				}

				scope.Set(name!, new XspFileSource<IXPathNavigable>(loaded.Value!, sources));
				scope.Context.TraceWriter.Current?.WriteXml(name!, loaded.Value!);
				return null;
			}

			string? fullKey = null;

			if (cacheKey != null)
			{
				fullKey = script.ScriptName.ShortName + "?" + cacheKey.Value.GetValue(scope);
				var cachedDoc = cache.Get<IXPathNavigable>(fullKey!);

				if (cachedDoc != null)
                {
					scope.Set(name!, cachedDoc);
					scope.Context.TraceWriter.Current.WriteXml(name!, cachedDoc.Value);
					return null;
				}
			}

			using var collector = scope.Context.BeginCollecting();

			var result = RenderStatements(script, scope);
			if (result.Error != null)
            {
				scope.Context.TraceWriter.Current.WriteError(result.Error);
				return result.Error;
            }

			if (fullKey != null)
            {
				cache.Set(fullKey, result.Value!, collector.FilesUsed, (int) cacheDuration!.Value.TotalSeconds);
            }

			scope.Context.TraceWriter.Current.WriteXml(name!, result.Value!);
			scope.Set(name!, new XspFileSource<IXPathNavigable>(result.Value!, collector.FilesUsed));

			return null;
		}

		private XspResult<IXPathNavigable> RenderStatements(XspScript script, XspScope scope)
        {
			var doc = new XmlDocument(script.Engine.ReaderSettings.NameTable!);

			using (XmlWriter xmlWriter = doc.CreateWriter())
			{
				xmlWriter.WriteStartDocument();
				xmlWriter.WriteStartElement("doc", "xml", "uri:doc");

				if (Statements != null)
				{
					var err = Statements.Execute(script, scope, xmlWriter);
					if (err != null)
					{
						return err;
					}
				}

				xmlWriter.WriteEndElement();
				xmlWriter.WriteEndDocument();
			}

			return doc;
		}
	}
}

