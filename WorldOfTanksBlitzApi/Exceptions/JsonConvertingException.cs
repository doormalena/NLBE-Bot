using System;

namespace WorldOfTanksBlitzApi.Exceptions
{
	[Serializable]
	public class JsonConvertingException : Exception
	{
		private static string message = "Something went wrong while trying to convert the json.";
		public JsonConvertingException() : base(message) { }

		public JsonConvertingException(string message)
			: base(message) { }

		public JsonConvertingException(string message, Exception inner)
			: base(message, inner) { }
	}
}
