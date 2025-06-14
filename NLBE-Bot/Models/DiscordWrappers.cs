namespace NLBE_Bot.Models;

using DSharpPlus.Entities;
using NLBE_Bot.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DiscordMessageWrapper(DiscordMessage message) : IDiscordMessage
{
	private readonly DiscordMessage _message = message;

	public IReadOnlyList<IDiscordReaction> Reactions =>
		_message.Reactions.Select(r => new DiscordReactionWrapper(r)).ToList();

	public Task<IReadOnlyList<IDiscordUser>> GetReactionsAsync(IDiscordEmoji emoji)
	{
		return _message.GetReactionsAsync(((DiscordEmojiWrapper) emoji).Inner)
			.ContinueWith(t => (IReadOnlyList<IDiscordUser>) [.. t.Result.Select(u => new DiscordUserWrapper(u))]);
	}

	public string Content => _message.Content;

	public DiscordMessage Inner => _message;
}

public class DiscordReactionWrapper(DiscordReaction reaction) : IDiscordReaction
{
	private readonly DiscordReaction _reaction = reaction;

	public IDiscordEmoji Emoji => new DiscordEmojiWrapper(_reaction.Emoji);

	public DiscordReaction Inner
	{
		get;
	} = reaction;
}

public class DiscordEmojiWrapper(DiscordEmoji emoji) : IDiscordEmoji
{
	public DiscordEmoji Inner
	{
		get;
	} = emoji;
}

public class DiscordUserWrapper(DiscordUser user) : IDiscordUser
{
	public DiscordUser Inner
	{
		get;
	} = user;
}
public class DiscordChannelWrapper(DiscordChannel channel) : IDiscordChannel
{
	private readonly DiscordChannel _channel = channel;

	public Task<IReadOnlyList<IDiscordMessage>> GetMessagesAsync(int limit = 100)
	{
		return _channel.GetMessagesAsync(limit)
			.ContinueWith(t => (IReadOnlyList<IDiscordMessage>) [.. t.Result.Select(u => new DiscordMessageWrapper(u))]);
	}

	public DiscordChannel Inner => _channel;
}

