using System;
using System.Xml;

namespace XSP.Engine.Schema.Statement
{
	public class XspCallStatement : XspStatement
	{
		public readonly XspRef Href;

		internal XspCallStatement(XmlReader reader, XspParser parser, XspResolver resolver)
			: base(new XspSource(reader, parser))
		{
			var href = reader.GetAttribute("href");
			if (href == null)
            {
				parser.Raise("xsp:call missing href=");
            }

			this.Href = XspRef.From(href!, resolver, parser.Script, Source);
		}

        public override XspError? Execute(XspScript script, XspScope scope, XmlWriter? writer)
        {
			if (Href.Script != null)
            {
				var scriptResult = scope.Context.GetScript(Href.Script);

				using var traceScope = scope.Context.TraceWriter.CreateScriptScope(Href.Script, scriptResult.Value);

				if (scriptResult.Error != null)
                {
					traceScope.WriteError(scriptResult.Error);
					return scriptResult.Error;
                }

				if (script != scriptResult.Value!)
                {
					return scriptResult.Value!.Execute(Href, scope, writer);
				}
            }

			return script.Execute(Href, scope, writer);
        }
    }
}

