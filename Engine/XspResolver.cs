using System;

namespace XSP.Engine
{
    public class XspResolver
	{
		private readonly string scriptRoot;

		public XspResolver(string scriptRoot)
		{
			this.scriptRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), scriptRoot));
		}

        public string Resolve(string path, string? pathContext)
        {
            if (path.StartsWith("/"))
            {
                return this.scriptRoot + path;
            }

            return Path.Combine(pathContext ?? scriptRoot, path);
        }

        public string ResolveAndSimplify(string path, string? pathContext)
        {
            return Simplify(Resolve(path, pathContext));
        }

        internal string Simplify(string scriptPath)
        {
            if (scriptPath == scriptRoot)
            {
                return string.Empty;
            }

            if (scriptPath.StartsWith(scriptRoot))
            {
                return scriptPath[(scriptRoot.Length + 1)..];
            }

            return scriptPath;
        }
    }
}

