using System;

namespace WorldOfTanksBlitzApi.Exceptions
{
	public class TooManyResultsException : Exception
	{
		private static string message = "Too many results were found!";
		public TooManyResultsException() : base(message) { }

		public TooManyResultsException(string message)
			: base(message) { }

		public TooManyResultsException(string message, Exception inner)
			: base(message, inner) { }

	}
}
