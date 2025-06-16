namespace NLBE_Bot.Interfaces;

using DSharpPlus.Entities;
using System;
using System.Collections.Generic;

internal interface IDiscordMessageUtils
{
	public Dictionary<IDiscordEmoji, List<IDiscordUser>> SortReactions(IDiscordMessage message);

	public Dictionary<DateTime, List<IDiscordMessage>> SortMessages(IReadOnlyList<IDiscordMessage> messages);

	public DiscordEmoji GetDiscordEmoji(string name);

	public string GetEmojiAsString(string emoji);
}
