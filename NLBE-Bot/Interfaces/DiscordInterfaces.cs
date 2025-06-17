namespace NLBE_Bot.Interfaces;

using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IDiscordMessage
{
	public IReadOnlyList<IDiscordReaction> Reactions
	{
		get;
	}
	public Task<IReadOnlyList<IDiscordUser>> GetReactionsAsync(IDiscordEmoji emoji);
	public string Content
	{
		get;
	}
	public DiscordMessage Inner
	{
		get;
	}
}
public interface IDiscordReaction
{
	public IDiscordEmoji Emoji
	{
		get;
	}
	public DiscordReaction Inner
	{
		get;
	}
}
public interface IDiscordEmoji
{
	public DiscordEmoji Inner
	{
		get;
	}

	public string GetDiscordName();
	public string ToString();
}
public interface IDiscordUser
{
	public DiscordUser Inner
	{
		get;
	}
}
public interface IDiscordChannel
{
	public Task<IReadOnlyList<IDiscordMessage>> GetMessagesAsync(int limit = 100);

	public DiscordChannel Inner
	{
		get;
	}
}
public interface ICommand
{
	public string Name
	{
		get;
	}
}

public interface ICommandContext
{
	public ulong GuildId
	{
		get;
	}

	public Task SendUnauthorizedMessageAsync();

	public Task DeleteInProgressReactionAsync(IDiscordEmoji emoji);

	public Task AddErrorReactionAsync(IDiscordEmoji emoji);
}
