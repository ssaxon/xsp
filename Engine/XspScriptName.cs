namespace XSP.Engine
{
    public struct XspScriptName
    {
		public string Name { get; private set; }
		public string Path { get; private set; }
		public string ShortName { get; private set; }
		public string ShortPath { get; private set; }

		public XspScriptName(string scriptPath, XspEngine engine)
        {
			this.Name = scriptPath;
			this.Path = System.IO.Path.GetDirectoryName(scriptPath)!;
			this.ShortName = engine.Resolver.Simplify(Name)!;
			this.ShortPath = engine.Resolver.Simplify(Path)!;
		}
	}
}

