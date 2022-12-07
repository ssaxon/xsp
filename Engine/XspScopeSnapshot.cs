namespace XSP.Engine
{
    internal class XspScopeSnapshot
    {
        private readonly Func<long> getSnapshot;
        private readonly long initial;

        internal XspScopeSnapshot(Func<long> getSnapshot)
        {
            this.getSnapshot = getSnapshot;
            this.initial = getSnapshot();
        }

        public bool HasChanged => initial != getSnapshot();
    }
}

