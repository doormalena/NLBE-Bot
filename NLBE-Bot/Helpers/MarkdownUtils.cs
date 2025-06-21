namespace NLBE_Bot.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;

internal static class MarkdownUtils
{
	public static List<Tuple<ulong, string>> RemoveSyntax(this List<Tuple<ulong, string>> stringList)
	{
		return stringList.Select(item => Tuple.Create(item.Item1, RemoveSyntax(item.Item2))).ToList();
	}

	public static string RemoveSyntax(this string stringItem)
	{
		stringItem = stringItem.Replace("\\", string.Empty);

		if (stringItem.StartsWith("**") && stringItem.EndsWith("**"))
		{
			return stringItem.Trim('*');
		}

		if (stringItem.StartsWith('`') && stringItem.EndsWith('`'))
		{
			return stringItem.Trim('`');
		}

		return stringItem;
	}
}
