namespace NLBE_Bot.Helpers;

using System.Text;
using System.Text.RegularExpressions;

public static class DiscordStringExtensions
{
	public static string AdaptToChat(this string text)
	{
		while (text.Contains("\\_"))
		{
			text = text.Replace("\\_", "_");
		}

		return text.Replace("_", "\\_");
	}

	public static string AdaptLink(this string text)
	{
		StringBuilder sb = new();
		string[] splitted = text.Split('\n');
		Regex regex = new(@"https?:\/\/[a-zA-Z0-9]*.[a-z\.]*\/?[a-zA-Z0-9\/\.?=&\-#]*", RegexOptions.NonBacktracking);

		foreach (string line in splitted)
		{
			string tempString = line.Replace("\\", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);

			if (!string.IsNullOrEmpty(line))
			{
				foreach (Match item in regex.Matches(tempString))
				{
					tempString = tempString.Replace(item.Value, "[" + item.Value + "](" + item.Value + ")");
				}
			}

			sb.AppendLine(tempString);
		}

		return sb.ToString();
	}

	public static string AdaptMutlipleLines(this string text)
	{
		while (text.Contains("\n\n\n") || text.Contains("\r\r\r") || text.Contains("\r\r\n") || text.Contains("\r\n\r") || text.Contains("\r\n\n") || text.Contains("\n\r\r") || text.Contains("\n\r\n") || text.Contains("\n\n\r"))
		{
			text = text.Replace("\r\r\r", "\n\n").Replace("\r\r\n", "\n\n").Replace("\r\n\r", "\n\n").Replace("\r\n\n", "\n\n").Replace("\n\r\r", "\n\n").Replace("\n\r\n", "\n\n").Replace("\n\n\r", "\n\n").Replace("\n\n\n", "\n\n");
		}

		return text;
	}
}
