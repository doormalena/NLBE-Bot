using System;

namespace DiscordHelper
{
	public static class Helper
	{
		public static string CreateTitle(string content, string url)
		{
			return String.Format("[{0}]({1})", content, url);
		}
	}
}
