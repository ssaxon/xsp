using System;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using XSP.Engine.Schema.Expressions;

namespace XSP.Engine.Schema.Statement
{
	public class XspQueryStatement : XspStatement
	{
		private readonly NodeOrString src;
		private readonly string[]? args;
		private readonly NodeOrString? select;
		private readonly IEnumerable<XspStatement>? statements;
		private readonly IDictionary<string, string>? namespaces;

		private XmlDocument? xsltDoc;

		internal XspQueryStatement(XmlReader reader, XspParser parser, XspResolver resolver)
			: base(new XspSource(reader, parser))
		{
			bool isEmpty = reader.IsEmptyElement;

			if (reader is IXmlNamespaceResolver xmlResolver)
            {
				namespaces = xmlResolver.GetNamespacesInScope(XmlNamespaceScope.ExcludeXml);
            }

			var src = reader.GetAttribute("src");
			if (src == null)
			{
				parser.Raise("xsp:query missing src=");
			}
			this.src = new NodeOrString(src);

			var args = reader.GetAttribute("args");
			if (args != null)
			{
				this.args = args.Split(' ');
			}

			var select = reader.GetAttribute("select");
			if (select != null)
			{
				this.select = new NodeOrString(select);
			}

			reader.MoveToElement();
			if (!isEmpty)
            {
				statements = parser.ParseStatements();
			}
		}

		public override XspError? Execute(XspScript script, XspScope scope, XmlWriter? writer)
		{
            IXPathNavigable? navigable;
			string src = this.src.GetValue(scope)!;

			bool usesXmlVariable = src.StartsWith("#");
			if (usesXmlVariable)
			{
				src = src[1..];
			}
			else
			{
				src = script.Engine.Resolver.ResolveAndSimplify(src!, script.ScriptName.Path);
			}

			if (usesXmlVariable)
			{
				if (!scope.ContainsKey(src))
                {
					return new XspError($"Unknown XML variable \"{src}\"");
                }

				var obj = scope[src];
				if (obj is not XspFileSource<IXPathNavigable> navigableVariable)
                {
					return new XspError($"Variable \"{src}\" does not contain XML");
				}

				scope.Context.RegisterFiles(navigableVariable.Sources);

				navigable = navigableVariable.Value;
			}
			else
			{
				var loaded = scope.Context.Engine.LoadXml(src, out IEnumerable<string> sources);
				if (loaded.Error != null)
				{
					scope.Context.TraceWriter.Current.WriteError(loaded.Error);
					return loaded.Error;
				}

				scope.Context.RegisterFiles(sources);
				navigable = loaded.Value!;
			}

			if (statements != null)
            {
				XmlDocument? xsltDoc = this.xsltDoc;

				if (xsltDoc == null)
				{
					var snapshot = scope.Snapshot();

					xsltDoc = new XmlDocument(script.Engine.ReaderSettings.NameTable!);

					using (XmlWriter xsltWriter = xsltDoc.CreateWriter())
					{
						xsltWriter.WriteStartDocument();
						xsltWriter.WriteStartElement("xsl", "stylesheet", XspNamespaces.XsltNamespaceURI);
						xsltWriter.WriteAttributeString("xmlns", "xs", null, XspNamespaces.XmlSchemaNamespaceURI);

						var exclude = new StringBuilder("xs");

						if (namespaces != null)
                        {
							foreach(var ns in namespaces)
                            {
								if (ns.Value != XspNamespaces.XsltNamespaceURI &&
									ns.Value != XspNamespaces.XmlSchemaNamespaceURI && ns.Value != "uri:xsp")
                                {
									exclude.Append(' ');
									exclude.Append(ns.Key);
									xsltWriter.WriteAttributeString("xmlns", ns.Key, null, ns.Value);
								}
							}
                        }

						xsltWriter.WriteAttributeString("exclude-result-prefixes", exclude.ToString());
						xsltWriter.WriteAttributeString("version", "2.0");

						var err = statements.Execute(script, scope, xsltWriter);
						if (err != null)
						{
							return err;
						}

						xsltWriter.WriteEndElement();
						xsltWriter.WriteEndDocument();
					}

					if (!snapshot.HasChanged)
					{
						this.xsltDoc = xsltDoc;
					}
				}

				XsltArgumentList? argumentList = new();
				argumentList.AddParam("dateTime", string.Empty, DateTime.UtcNow.ToIsoDate());

				if (this.args != null)
                {
					foreach (var key in this.args)
                    {
						if (key == "query")
                        {
							foreach(var pair in scope.Context.QueryArguments)
                            {
								argumentList.AddParam(pair.Key, string.Empty, pair.Value!);
							}
                        }
						else if (key.StartsWith("query."))
						{
							var name = key[6..];
							if (scope.Context.QueryArguments.TryGetValue(name, out object? value))
							{
								argumentList.AddParam(name, string.Empty, value!);
							}
						}
						else if (scope.TryGetValue(key, out object? value))
                        {
							argumentList.AddParam(key, string.Empty, value);
						}
					}
                }

				var xslTransform = new XslTransform();
				xslTransform.Load(xsltDoc.CreateNavigator()!);
				ProcessSelect(navigable, select, scope,
					nav => xslTransform.Transform(nav, argumentList, writer!));
			}
			else if (usesXmlVariable)
			{
				ProcessSelect(navigable, select != null ? select : "/*/node()", scope,
					nav => nav.WriteSubtree(writer!));
			}
			else
			{
				ProcessSelect(navigable, select != null ? select : "/", scope,
					nav => nav.WriteSubtree(writer!));
			}

			return null;
		}

		private static void ProcessSelect(IXPathNavigable navigable, NodeOrString? select, XspScope scope, Action<XPathNavigator> action)
        {
			var nav = navigable.CreateNavigator()!;
			if (select == null)
            {
				action(nav);
				return;
            }

			var selectStr = select.Value.GetValue(scope);

			XmlNamespaceManager namespaceManager = new XmlNamespaceManager(nav.NameTable);
			foreach(var ns in nav.GetNamespacesInScope(XmlNamespaceScope.All))
            {
				namespaceManager.AddNamespace(ns.Key, ns.Value);
            }

			XPathNodeIterator iter = nav.Select(selectStr!, namespaceManager);
			while (iter.MoveNext())
            {
				action(iter.Current!);
            }
		}
    }

	class SwitchableStringWriter : StringWriter
    {
		public bool Enabled { get; set; } = true;

		public SwitchableStringWriter(bool enabled = true) : base()
		{
			Enabled = enabled;
		}

		public SwitchableStringWriter(StringBuilder sb, bool enabled = true): base(sb)
        {
			Enabled = enabled;
        }

		public override void Write(char value)
		{
			if (Enabled)
			{ 
				base.Write(value);
			}
		}

		public override void Write(char[] buffer, int index, int count)
		{
			if (Enabled)
			{
				base.Write(buffer, index, count);
			}
		}

		public override void Write(String? value)
		{
			if (Enabled)
			{
				base.Write(value);
			}
		}
	}
}

