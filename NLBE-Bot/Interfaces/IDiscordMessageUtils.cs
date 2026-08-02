namespace NLBE_Bot.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal interface IDiscordMessageUtils
{
	public Task<Dictionary<IDiscordEmoji, List<IDiscordUser>>> SortReactions(IDiscordMessage message);

	public Dictionary<DateTime, List<IDiscordMessage>> SortMessages(IReadOnlyList<IDiscordMessage> messages);

	public IDiscordEmoji? GetDiscordEmoji(string name);

	public string GetEmojiAsString(string emoji);
}
