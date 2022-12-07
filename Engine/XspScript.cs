using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using XSP.Engine.Schema;
using XSP.Engine.Schema.Statement;

namespace XSP.Engine
{
    public class XspScript
	{
		public XspScriptName ScriptName { get; private set; }
		public XspEngine Engine { get; private set; }

		private XspScope fileScope;
		private readonly Dictionary<string, XspSub> subroutines = new();

        private XspScript(string scriptPath, XspEngine engine)
		{
			this.ScriptName = new XspScriptName(scriptPath, engine);
			this.Engine = engine;
		}

		public string? BaseRef { get; private set; }

		internal static XspScript Load(string path, XspEngine engine, XspContext context)
        {
			if (!File.Exists(path))
            {
				throw new FileNotFoundException("Script not found", path);
            }

			var script = new XspScript(path, engine);
			using var reader = XmlReader.Create(File.OpenRead(path), engine.ReaderSettings);
			var parser = new XspParser(reader, script);

			var @base = reader.GetAttribute("base");
			if (@base != null)
            {
				script.BaseRef = engine.Resolver.ResolveAndSimplify(@base!, script.ScriptName.Path);
			}

			parser.Parse(context);

			return script;
		}

		public XspResult<bool> TryGetSub(string subName, XspScope scope, [MaybeNullWhen(false)] out XspSub sub)
        {
			if (subroutines.TryGetValue(subName, out sub))
            {
				return true;
            }

			if (this.BaseRef == null)
            {
				return false;
            }

			var baseScript = scope.Context.GetScript(this.BaseRef);
			if (baseScript.Error != null)
            {
				scope.Context.TraceWriter.Current.WriteError(baseScript.Error);
				return baseScript.Error;
            }

			return baseScript.Value!.TryGetSub(subName, scope, out sub);
		}

		public XspError? Execute(XspRef scriptRef, XspScope scope, XmlWriter? writer)
        {
			var subName = scriptRef.Sub ?? "main";
			var getSub = TryGetSub(subName, scope, out XspSub? sub);

			if (getSub.Error != null)
            {
				scope.Context.TraceWriter.Current.WriteError(getSub.Error);
				return getSub.Error;
            }

			if (!getSub.Value)
			{
				var err = new XspError($"Cannot find {scriptRef.Full}");
				scope.Context.TraceWriter.Current.WriteError(err);
				return err;
			}

			using var traceScope = scope.Context.TraceWriter.Current.CreateSubScope(subName, sub.Script!.ScriptName);

			return scope.Context.WithScope(
				scriptRef,
				childScope => sub!.Statements.Execute(this, childScope, writer),
				parentScope: sub!.FileScope);
        }

		internal void AddSub(XspSub sub)
        {
			sub.Script = this;
			subroutines.Add(sub.Name, sub);
		}

		internal XspScope FileScope {
			get { return fileScope; }
			set
            {
				this.fileScope = value;
				foreach (var sub in subroutines.Values)
                {
					sub.FileScope = value;
                }
            }
		}
	}
}

