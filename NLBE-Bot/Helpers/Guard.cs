namespace NLBE_Bot.Helpers;

using Microsoft.Extensions.Logging;

internal static class Guard
{
	/// <summary>
	/// Checks if the given object is null. If so, logs an error and returns true.
	/// Otherwise, assigns the object to the out parameter and returns false.
	/// </summary>
	public static bool ReturnIfNull<T>(T? obj, ILogger logger, string name, out T result)
		where T : class
	{
		if (obj == null)
		{
			logger.LogWarning("{Name} is missing.", name);
			result = null!;
			return true;
		}

		result = obj;
		return false;
	}
}
