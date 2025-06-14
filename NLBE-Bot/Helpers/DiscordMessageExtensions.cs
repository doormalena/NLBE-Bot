namespace NLBE_Bot.Helpers;

using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

public static class DiscordMessageExtensions
{
	public static Dictionary<IDiscordEmoji, List<IDiscordUser>> SortReactions(this IDiscordMessage message)
	{
		return message.Reactions.ToDictionary(
			reaction => reaction.Emoji,
			reaction => message.GetReactionsAsync(reaction.Emoji).Result.ToList()
		);
	}

	public static Dictionary<DateTime, List<IDiscordMessage>> SortMessages(this IReadOnlyList<IDiscordMessage> messages)
	{
		Dictionary<DateTime, List<IDiscordMessage>> sortedMessages = [];
		foreach (IDiscordMessage message in messages)
		{
			string[] splitted = message.Content.Split(Constants.LOG_SPLIT_CHAR);

			// TODO: refactor date parsing by using try parse, localization, and date-time formats.
			string[] dateTimeSplitted = splitted[0].Split(' ');
			string[] dateSplitted = dateTimeSplitted[0].Split('-');
			string[] timeSplitted = dateTimeSplitted[1].Split(':');
			DateTime date = new(Convert.ToInt32(dateSplitted[2]), Convert.ToInt32(dateSplitted[1]), Convert.ToInt32(dateSplitted[0]), Convert.ToInt32(timeSplitted[0]), Convert.ToInt32(timeSplitted[1]), Convert.ToInt32(timeSplitted[2]), DateTimeKind.Local);

			bool containsItem = false;
			foreach (KeyValuePair<DateTime, List<IDiscordMessage>> item in sortedMessages)
			{
				if (item.Key == date)
				{
					containsItem = true;
					item.Value.Add(message);
				}
			}
			if (!containsItem)
			{
				List<IDiscordMessage> tempMessageList = [message];
				sortedMessages.Add(date, tempMessageList);
			}
		}
		return sortedMessages;
	}
}
