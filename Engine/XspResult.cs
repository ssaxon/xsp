using System;

namespace XSP.Engine
{
	public class XspResult<T>
    {
		public readonly T? Value;
		public readonly XspError? Error;

		public XspResult(T value): this(value, null)
		{
		}

		public XspResult(XspError error) : this(default, error)
		{
		}

		public XspResult(T? value, XspError? error)
		{
			Value = value;
			Error = error;
		}

		public static implicit operator XspResult<T>(T value)
		{
			return new XspResult<T>(value);
		}

		public static implicit operator XspResult<T>(XspError error)
        {
			return new XspResult<T>(error);
        }

		public static XspResult<T> SafeCall(Func<T> factory)
        {
			try
            {
				return new XspResult<T>(factory());
            }
			catch(XspException ex)
            {
				return new XspResult<T>(ex.Error);
            }
			catch(Exception ex)
            {
				return new XspResult<T>(new XspError(ex));
			}
        }
	}
}

