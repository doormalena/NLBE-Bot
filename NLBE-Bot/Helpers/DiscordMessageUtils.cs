namespace NLBE_Bot.Helpers;

using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Linq;

internal class DiscordMessageUtils(IDiscordClientWrapper discordClient, IErrorHandler errorHandler, ILogger<DiscordMessageUtils> logger) : IDiscordMessageUtils
{
	private readonly IDiscordClientWrapper _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly ILogger<DiscordMessageUtils> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public Dictionary<IDiscordEmoji, List<IDiscordUser>> SortReactions(IDiscordMessage message)
	{
		return message.Reactions.ToDictionary(
			reaction => reaction.Emoji,
			reaction => message.GetReactionsAsync(reaction.Emoji).Result.ToList()
		);
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

	public IDiscordEmoji GetDiscordEmoji(string name)
	{
		DiscordEmoji theEmoji;

		try
		{
			theEmoji = DiscordEmoji.FromName(_discordClient.Inner, name);
			return new DiscordEmojiWrapper(theEmoji);
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, ex.Message);
		}

		theEmoji = DiscordEmoji.FromUnicode(name);

		if (theEmoji != null)
		{
			return new DiscordEmojiWrapper(theEmoji);
		}

		try
		{
			theEmoji = DiscordEmoji.FromName(_discordClient.Inner, name);
		}
		catch (Exception ex)
		{
			_errorHandler.HandleErrorAsync("Could not load emoji:", ex).Wait();
		}

		return new DiscordEmojiWrapper(theEmoji);
	}

	public string GetEmojiAsString(string emoji)
	{
		IDiscordEmoji theEmoji = GetDiscordEmoji(emoji);

		if (!theEmoji.GetDiscordName().Equals(emoji))
		{
			return theEmoji.GetDiscordName();
		}

		try
		{
			return DiscordEmoji.FromUnicode(_discordClient.Inner, emoji).Name;
		}
		catch
		{
			return emoji;
		}
	}
}
