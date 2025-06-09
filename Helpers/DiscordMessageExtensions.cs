namespace NLBE_Bot.Helpers;

using DSharpPlus.Entities;
using System;
using System.Collections.Generic;

public static class DiscordMessageExtensions
{
	public static Dictionary<DiscordEmoji, List<DiscordUser>> SortReactions(this DiscordMessage message)
	{
		Dictionary<DiscordEmoji, List<DiscordUser>> sortedReactions = [];
		foreach (DiscordReaction reaction in message.Reactions)
		{
			DiscordEmoji emoji = reaction.Emoji;
			IReadOnlyList<DiscordUser> users = message.GetReactionsAsync(reaction.Emoji).Result;
			List<DiscordUser> userList = [.. users];
			sortedReactions.Add(emoji, userList);
		}
		return sortedReactions;
	}

	public static Dictionary<DateTime, List<DiscordMessage>> SortMessages(this IReadOnlyList<DiscordMessage> messages)
	{
		Dictionary<DateTime, List<DiscordMessage>> sortedMessages = [];
		foreach (DiscordMessage message in messages)
		{
			string[] splitted = message.Content.Split(Constants.LOG_SPLIT_CHAR);
			string[] dateTimeSplitted = splitted[0].Split(' ');
			string[] dateSplitted = dateTimeSplitted[0].Split('-');
			string[] timeSplitted = dateTimeSplitted[1].Split(':');
			DateTime date = new(Convert.ToInt32(dateSplitted[2]), Convert.ToInt32(dateSplitted[1]), Convert.ToInt32(dateSplitted[0]), Convert.ToInt32(timeSplitted[0]), Convert.ToInt32(timeSplitted[1]), Convert.ToInt32(timeSplitted[2]), DateTimeKind.Local);

			bool containsItem = false;
			foreach (KeyValuePair<DateTime, List<DiscordMessage>> item in sortedMessages)
			{
				string xdate = item.Key.ConvertToDate();
				string ydate = date.ConvertToDate();
				if (xdate.Equals(ydate))
				{
					containsItem = true;
					item.Value.Add(message);
				}
			}
			if (!containsItem)
			{
				List<DiscordMessage> tempMessageList = [message];
				sortedMessages.Add(date, tempMessageList);
			}
		}
		return sortedMessages;
	}
}
