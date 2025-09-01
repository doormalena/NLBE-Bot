namespace NLBE_Bot.Interfaces;

using System;
using System.Collections.Generic;

internal interface IDiscordMessageUtils
{
	public Dictionary<IDiscordEmoji, List<IDiscordUser>> SortReactions(IDiscordMessage message);

	public Dictionary<DateTime, List<IDiscordMessage>> SortMessages(IReadOnlyList<IDiscordMessage> messages);

	public IDiscordEmoji? GetDiscordEmoji(string name);

	public string GetEmojiAsString(string emoji);
}
