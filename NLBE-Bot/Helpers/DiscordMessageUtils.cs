namespace NLBE_Bot.Helpers;

using DSharpPlus.Entities;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal class DiscordMessageUtils(IDiscordClient discordClient) : IDiscordMessageUtils
{
	private readonly IDiscordClient _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));

	public async Task<Dictionary<IDiscordEmoji, List<IDiscordUser>>> SortReactions(IDiscordMessage message)
	{
		Dictionary<IDiscordEmoji, List<IDiscordUser>> result = [];

		foreach (IDiscordReaction reaction in message.Reactions)
		{
			IReadOnlyList<IDiscordUser> users = await message.GetReactionsAsync(reaction.Emoji);
			result[reaction.Emoji] = [.. users];
		}

		return result;
	}

	public Dictionary<DateTime, List<IDiscordMessage>> SortMessages(IReadOnlyList<IDiscordMessage> messages)
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

	public IDiscordEmoji? GetDiscordEmoji(string name)
	{
		if (_discordClient.Inner != null && DiscordEmoji.TryFromName(_discordClient.Inner, name, out DiscordEmoji theEmoji))
		{
			return new DiscordEmojiWrapper(theEmoji);
		}

		if (!string.IsNullOrEmpty(name) && DiscordEmoji.TryFromUnicode(name, out theEmoji))
		{
			return new DiscordEmojiWrapper(theEmoji);
		}

		return null;
	}

	public string GetEmojiAsString(string emoji)
	{
		if (string.IsNullOrEmpty(emoji))
		{
			return string.Empty;
		}

		IDiscordEmoji? theEmoji = GetDiscordEmoji(emoji);

		if (theEmoji != null && !theEmoji.GetDiscordName().Equals(emoji))
		{
			return theEmoji.GetDiscordName();
		}

		return emoji;
	}
}
