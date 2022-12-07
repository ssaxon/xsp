using System;
using System.Collections.Concurrent;

namespace XSP.Engine
{
    public class XspContext
	{
        private XspScope global;
        private Stack<XspScope> scopes = new();
        private HashSet<XspContextCollector> collectors = new();
        private ConcurrentDictionary<string, XspResult<XspScript>> scriptsInContext = new();

		public readonly XspEngine Engine;

		public XspContext(XspEngine engine, IReadOnlyDictionary<string, object?> queryArguments, XspLocale locale, XspTraceWriter traceWriter)
		{
			Engine = engine;
            QueryArguments = queryArguments;
            Locale = locale;
            TraceWriter = traceWriter;

            global = new(".global", this);

            scopes.Push(global);
		}

        public IReadOnlyDictionary<string, object?> QueryArguments { get; private set; }
        public XspLocale Locale { get; private set; }
        public XspTraceWriter TraceWriter { get; private set; }

        public XspScope CurrentScope { get { return scopes.Peek(); } }
        public XspScope GlobalScope { get { return global; } }

        public XspResult<XspScript> GetScript(string scriptName)
        {
            return scriptsInContext.GetOrAdd(scriptName, scriptName =>
                Engine.LoadScript(scriptName, this));
        }

        public void WithScope(string scopeName, Action<XspScope> action, XspScope? parentScope = null)
        {
            var scope = PushScope(scopeName, parentScope: parentScope);
            try
            {
                action(scope);
            }
            finally
            {
                PopScope();
            }
        }

        public T WithScope<T>(XspRef xspRef, Func<XspScope, T> func, XspScope? parentScope = null)
        {
            var scope = PushScope(xspRef.Full, xspRef, parentScope: parentScope);
            try
            {
                return func(scope);
            }
            finally
            {
                PopScope();
            }
        }

        private XspScope PushScope(String scopeName, XspRef? xspRef = null, XspScope? parentScope = null)
        {
			var scope = new XspScope(scopeName, this, parentScope ?? global, xspRef, CurrentScope);
			scopes.Push(scope);
			return scope;
		}

        private XspScope PopScope()
        {
			return scopes.Pop();
        }

        internal IXspContextCollector BeginCollecting()
        {
            var collector = new XspContextCollector(c => collectors.Remove(c));
            this.collectors.Add(collector);
            return collector;
        }

        internal void RegisterFiles(IEnumerable<string> files)
        {
            foreach(var c in collectors)
            {
                c.RegisterFiles(files);
            }
        }

        class XspContextCollector : IXspContextCollector
        {
            private bool disposedValue;
            private readonly Action<XspContextCollector> onDispose;
            private readonly HashSet<string> filesUsed = new();

            public IEnumerable<string> FilesUsed => filesUsed;

            public XspContextCollector(Action<XspContextCollector> onDispose)
            {
                this.onDispose = onDispose;
            }

            public void RegisterFiles(IEnumerable<string> files)
            {
                foreach(var file in files)
                {
                    filesUsed.Add(file);
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        onDispose(this);
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }

    internal interface IXspContextCollector : IDisposable
    {
        IEnumerable<string> FilesUsed { get; }
    }

}

