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
	public Task<IReadOnlyList<IDiscordMessage>> GetMessagesAsync(int limit);

	public DiscordChannel Inner
	{
		get;
	}
}
