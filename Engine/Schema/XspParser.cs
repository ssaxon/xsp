using System.Diagnostics;
using System.Xml;
using XSP.Engine.Schema.Statement;

namespace XSP.Engine.Schema
{
    internal class XspParser
    {
		private readonly XmlReader reader;
		public readonly string XspNamespace;
		public readonly XspScript Script;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public XspParser(XmlReader reader, XspScript script)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
			this.reader = reader;
            Script = script;

            while (reader.Read())
            {
				if (reader.NodeType == XmlNodeType.Element)
                {
					XspNamespace = reader.NamespaceURI;

					if (reader.LocalName != "script")
                    {
                        Raise("Expected <xsp:script>");
					}
					return;
				}
            }

            Raise("Missing <xsp:script>");
        }

        public void ParseElements(Action<XmlNodeType> action, bool allowText, bool allowEOF)
        {
            var namespaceURI = reader.NamespaceURI;
            var name = reader.LocalName;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Comment)
                {
                    continue;
                }

                if (allowText && reader.NodeType == XmlNodeType.Text)
                {
                    // good to go
                }
                else if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (reader.NamespaceURI != namespaceURI || reader.LocalName != name)
                    {
                        Raise($"Unexpected close element \"{reader.Name}\"");
                    }

                    return;
                }
                else if (reader.NodeType != XmlNodeType.Element)
                {
                    Raise($"Unexpected node of type \"{reader.NodeType}\"");
                }

                action(reader.NodeType);
            }

            if (!allowEOF)
            {
                Raise("Unexpected end of file");
            }
        }

        public void Parse(XspContext context)
        {
            Lazy<List<XspStatement>> initStatements = new(() => new List<XspStatement>());
            bool hasSub = false;

            ParseElements(_ =>
            {
                switch (reader.LocalName)
                {
                    case "assign":
                        if (hasSub)
                        {
                            Raise($"xsp:assign cannot appear after xsp:sub");
                        }
                        initStatements.Value.Add(new XspAssignStatement(reader, this, Script.Engine.Resolver));
                        break;

                    case "xml":
                        if (hasSub)
                        {
                            Raise($"xsp:xml cannot appear after xsp:sub");
                        }
                        initStatements.Value.Add(new XspXmlStatement(reader, this, Script.Engine.Resolver));
                        break;

                    case "sub":
                        hasSub = true;

                        var subName = reader.GetAttribute("name");
                        if (subName == null)
                        {
                            Raise($"Missing name= on \"{reader.Name}\"");
                        }

                        var statements = ParseStatements();
                        Script.AddSub(new XspSub(subName!, statements));
                        break;

                    default:
                        Raise($"Unexpected element \"{reader.Name}\"");
                        break;

                }
            }, false, true);

            context.WithScope(Script.ScriptName.ShortName, scope =>
            {
                scope.Set("script", Script.ScriptName);

                if (initStatements.IsValueCreated)
                {
                    initStatements.Value.Execute(Script, context.CurrentScope, null);                
                }

                Script.FileScope = scope;
            });
        }

        [DebuggerHidden]
        public void Raise(string message)
        {
            throw new XspException(message, Script, reader);
        }

        private XspSource CreateSource()
        {
            return new XspSource(reader, this);
        }

        public IEnumerable<XspStatement> ParseStatements()
        {
            var statements = new List<XspStatement>();

            if (reader.IsEmptyElement)
            {
                return statements;
            }

            ParseElements(type =>
            {
                switch (type)
                {
                    case XmlNodeType.Text:
                        statements.Add(new XspLiteralStatement(CreateSource(), reader.Value));
                        break;

                    case XmlNodeType.Element:
                        if (reader.NamespaceURI == XspNamespace)
                        {
                            statements.Add(ParseStatement());
                        } else
                        {
                            statements.Add(new XElementStatement(reader, this));
                        }
                        break;

                    default:
                        Raise($"Unexpected statement type \"{type}\"");
                        break;
                }
            }, true, false);

            return statements;
        }

        private XspStatement ParseStatement()
        {
            switch (reader.LocalName)
            {
                case "assign":
                    return new XspAssignStatement(reader, this, Script.Engine.Resolver);

                case "call":
                    return new XspCallStatement(reader, this, Script.Engine.Resolver);

                case "choose":
                    return new XspChooseStatement(reader, this, Script.Engine.Resolver);

                case "if":
                    return new XspIfStatement(reader, this, Script.Engine.Resolver);

                case "query":
                    return new XspQueryStatement(reader, this, Script.Engine.Resolver);

                case "xml":
                    return new XspXmlStatement(reader, this, Script.Engine.Resolver);

                default:
                    Raise($"Unexpected XSP statement \"{reader.Name}\"");
                    return null!;
            }
        }
    }
}

