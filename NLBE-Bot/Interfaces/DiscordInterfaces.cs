namespace NLBE_Bot.Interfaces;

using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IDiscordClient
{
	public DiscordClient Inner
	{
		get;
	}
	public IReadOnlyDictionary<ulong, IDiscordGuild> Guilds
	{
		get;
	}
	public Task ConnectAsync(DiscordActivity activity, UserStatus status);
	public ICommandsNextExtension UseCommandsNext(CommandsNextConfiguration config);
	public ICommandsNextExtension GetCommandsNext();
	public Task<DiscordUser> GetUserAsync(ulong userId);
	public Task<DiscordGuild> GetGuildAsync(ulong guildId);
	public InteractivityExtension GetInteractivity();
	public Task<DiscordMessage> SendMessageAsync(DiscordChannel channel, string content, DiscordEmbed embed = null);

	public event AsyncEventHandler<DiscordClient, ReadyEventArgs> Ready;
	public event AsyncEventHandler<DiscordClient, HeartbeatEventArgs> Heartbeated;
	public event AsyncEventHandler<DiscordClient, MessageCreateEventArgs> MessageCreated;
	public event AsyncEventHandler<DiscordClient, MessageDeleteEventArgs> MessageDeleted;
	public event AsyncEventHandler<DiscordClient, MessageReactionAddEventArgs> MessageReactionAdded;
	public event AsyncEventHandler<DiscordClient, MessageReactionRemoveEventArgs> MessageReactionRemoved;
	public event AsyncEventHandler<DiscordClient, GuildMemberAddEventArgs> GuildMemberAdded;
	public event AsyncEventHandler<DiscordClient, GuildMemberUpdateEventArgs> GuildMemberUpdated;
	public event AsyncEventHandler<DiscordClient, GuildMemberRemoveEventArgs> GuildMemberRemoved;

}
public interface ICommandsNextExtension
{
	public void RegisterCommands<T>() where T : BaseCommandModule;
	public event AsyncEventHandler<CommandsNextExtension, CommandExecutionEventArgs> CommandExecuted;
	public event AsyncEventHandler<CommandsNextExtension, CommandErrorEventArgs> CommandErrored;
}

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

public interface IDiscordGuild
{
	public ulong Id
	{
		get;
	}
	public DiscordGuild Inner
	{
		get;
	}
	public IReadOnlyDictionary<ulong, DiscordChannel> Channels
	{
		get;
	}
	public IReadOnlyDictionary<ulong, DiscordRole> Roles
	{
		get;
	}
	public Task<IReadOnlyCollection<DiscordMember>> GetAllMembersAsync();
	public Task<DiscordMember> GetMemberAsync(ulong userId);
	public Task LeaveAsync();
}
