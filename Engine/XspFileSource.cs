namespace XSP.Engine
{
    internal class XspFileSource<T>
    {
		public T Value { get; private set; }
		public IEnumerable<string> Sources { get; private set; }

		public XspFileSource(T value, IEnumerable<string> sources)
        {
			Value = value;
			Sources = sources;
        }
    }
}

