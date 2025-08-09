using System.Text;
using System.Text.RegularExpressions;

namespace DiscordHelper
{
	public static class StringHelper
	{
		public static string adaptToDiscordChat(this string text)
		{
			while (text.Contains("\\_"))
			{
				text = text.Replace("\\_", "_");
			}
			return text.Replace("_", "\\_");
		}
		public static string adaptDiscordLink(this string text)
		{
			StringBuilder sb = new StringBuilder();
			string[] splitted = text.Split('\n');
			foreach (string line in splitted)
			{
				string tempString = line.Replace("\\", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
				if (!line.Equals(string.Empty))
				{
					Regex regex = new Regex(@"https?:\/\/[a-zA-Z0-9]*.[a-z\.]*\/?[a-zA-Z0-9\/\.?=&\-#]*");
					MatchCollection matches = regex.Matches(tempString);
					if (matches.Count > 0)
					{
						foreach (Match item in matches)
						{
							tempString = tempString.Replace(item.Value, "[" + item.Value + "](" + item.Value + ")");
						}
					}
				}
				sb.AppendLine(tempString);
			}
			return sb.ToString();
			//string returnstring = Regex.Replace(text.Replace("\\", string.Empty), Bot.LINK_REGEX, "kassmet_ [$1]");
			//return returnstring;
		}
		public static string adaptMutlipleLines(this string text)
		{
			while (text.Contains("\n\n\n") || text.Contains("\r\r\r") || text.Contains("\r\r\n") || text.Contains("\r\n\r") || text.Contains("\r\n\n") || text.Contains("\n\r\r") || text.Contains("\n\r\n") || text.Contains("\n\n\r"))
			{
				text = text.Replace("\r\r\r", "\n\n").Replace("\r\r\n", "\n\n").Replace("\r\n\r", "\n\n").Replace("\r\n\n", "\n\n").Replace("\n\r\r", "\n\n").Replace("\n\r\n", "\n\n").Replace("\n\n\r", "\n\n").Replace("\n\n\n", "\n\n");
			}
			return text;
		}
	}
}
