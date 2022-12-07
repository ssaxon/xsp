using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSP.Engine.Schema.Expressions
{
    public class ExpressionException : Exception
    {
        public ExpressionException(Exception cause)
            : base(cause.Message, cause)
        {
        }
    }
}
