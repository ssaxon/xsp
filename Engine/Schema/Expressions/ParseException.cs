namespace XSP.Engine.Schema.Expressions
{
    public class ParseException : Exception
	{
        private readonly int position;

        public ParseException(string message, int position)
            : base(message)
		{
			this.position = position;
		}

        public int Position => this.position;
    }
}