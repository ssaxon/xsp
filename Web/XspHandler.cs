using System;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using XSP.Engine;

namespace XSP.Web
{
	public class XspHandler
	{
		private readonly RequestDelegate next;
		private readonly Regex pathMatch = new("^/(?<lang>[a-zA-Z]{2})?(?<path>/.*)?");
		private readonly XspEngine engine;

		public XspHandler(RequestDelegate next, XspEngine engine)
		{
			this.next = next;
			this.engine = engine;
		}

		public async Task Invoke(HttpContext context)
        {
			var path = context.Request.Path;

			if (!path.HasValue)
            {
				await next.Invoke(context);
				return;
			}

			var match = pathMatch.Match(path.Value!);
			var lang = match.Groups["lang"].Value.ToLower();
			var pathScript = match.Groups["path"].Value;

			if (lang.Length == 0)
            {
				lang = "en";
            }

			if (pathScript.Length == 0)
            {
				pathScript = "/default";
            }

			pathScript += ".xsp.xml";

			var outType = context.Request.Query["~out"];
			XmlDocument? traceDoc = null;
			XmlWriter? traceWriter = null;
			bool tracingMode = "trace".Equals(outType, StringComparison.InvariantCultureIgnoreCase);

			if (tracingMode)
            {
				traceDoc = new XmlDocument(engine.ReaderSettings.NameTable!);
				traceWriter = traceDoc.CreateWriter();
			}

			XspTraceWriter tracing = new(traceWriter);

			var xspContext = new XspContext(engine, context.Request.Query
				.Where(p => p.Key.Equals("~out", StringComparison.InvariantCultureIgnoreCase))
				.ToImmutableDictionary(
					pair => pair.Key,
					pair => (object?) pair.Value.FirstOrDefault()),
				new XspLocale(lang), tracing);
			xspContext.GlobalScope.Set("dateTime", DateTime.UtcNow.ToIsoDate());

			tracing.Begin();

			var result = engine.Execute("/scripts" + pathScript, xspContext);

			tracing.End();

			XmlDocument doc;
			if (!tracingMode && result.Error != null)
            {
				doc = new XmlDocument(engine.ReaderSettings.NameTable!);

				using XmlWriter writer = doc.CreateWriter();

				writer.WriteStartDocument();
				writer.WriteStartElement("doc", "error", "uri:doc");
				writer.WriteElementString("doc", "message", "uri:doc", result.Error.Message);

				if (result.Error.Exception != null)
                {
					writer.WriteStartElement("doc", "exception", "uri:doc");
					writer.WriteAttributeString("type", result.Error.Exception.GetType().Name);
					writer.WriteAttributeString("fullType", result.Error.Exception.GetType().FullName);
					writer.WriteElementString("message", result.Error.Exception.Message);
					writer.WriteEndElement();
				}

				writer.WriteEndElement();
				writer.WriteEndDocument();
			}
			else
            {
				doc = result.Value!;
            }

			if ("raw".Equals(outType, StringComparison.InvariantCultureIgnoreCase))
            {
				context.Response.Headers.ContentType = "application/xml";

				using var outputWriter = XmlWriter.Create(context.Response.BodyWriter.AsStream());
				doc.WriteContentTo(outputWriter);
			}
			else if (tracingMode)
			{
				traceWriter!.Dispose();

				context.Response.Headers.ContentType = "application/xml";

				using var outputWriter = XmlWriter.Create(context.Response.BodyWriter.AsStream());
				traceDoc!.WriteContentTo(outputWriter);
			}
			else
			{
				context.Response.Headers.ContentType = "text/html";

				var xslt = engine.LoadXsltFile("/main.xsl", 60 * 5);
				if (xslt.Error != null)
                {
					context.Response.StatusCode = 500;
					await context.Response.WriteAsync(xslt.Error.Message);
					return;
				}

				xslt.Value!.Transform(doc!, null, context.Response.BodyWriter.AsStream());
			}
		}
	}
}

