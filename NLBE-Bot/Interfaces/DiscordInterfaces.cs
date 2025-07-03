namespace NLBE_Bot.Interfaces;

using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Net.Models;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal interface IDiscordClient
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
	public Task<IDiscordUser> GetUserAsync(ulong userId);
	public Task<IDiscordGuild> GetGuildAsync(ulong guildId);
	public IDiscordInteractivityExtension GetInteractivity();
	public Task<IDiscordMessage> SendMessageAsync(IDiscordChannel channel, string content, IDiscordEmbed embed = null);
	public Task DisconnectAsync();

	public event AsyncEventHandler<DiscordClient, ReadyEventArgs> Ready;
	public event AsyncEventHandler<DiscordClient, HeartbeatEventArgs> Heartbeated;
	public event AsyncEventHandler<DiscordClient, MessageCreateEventArgs> MessageCreated;
	public event AsyncEventHandler<DiscordClient, MessageDeleteEventArgs> MessageDeleted;
	public event AsyncEventHandler<DiscordClient, MessageReactionAddEventArgs> MessageReactionAdded;
	public event AsyncEventHandler<DiscordClient, MessageReactionRemoveEventArgs> MessageReactionRemoved;
	public event AsyncEventHandler<DiscordClient, GuildMemberAddEventArgs> GuildMemberAdded;
	public event AsyncEventHandler<DiscordClient, GuildMemberUpdateEventArgs> GuildMemberUpdated;
	public event AsyncEventHandler<DiscordClient, GuildMemberRemoveEventArgs> GuildMemberRemoved;
	public event AsyncEventHandler<DiscordClient, ClientErrorEventArgs> ClientErrored;
	public event AsyncEventHandler<DiscordClient, SocketCloseEventArgs> SocketClosed;

}
internal interface IDiscordInteractivityExtension
{
	public Task<IDiscordInteractivityResult<IDiscordMessage>> WaitForMessageAsync(Func<IDiscordMessage, bool> value);
}

internal interface IDiscordInteractivityResult<out T>
{
	public bool TimedOut
	{
		get;
	}
	public T Result
	{
		get;
	}
}

internal interface ICommandsNextExtension
{
	public void RegisterCommands<T>() where T : BaseCommandModule;

	public Command FindCommand(string commandName, out string rawArguments);

	public CommandContext CreateContext(IDiscordMessage message, string prefix, Command command, string args);

	public Task ExecuteCommandAsync(CommandContext ctx);

	public IReadOnlyDictionary<string, IDiscordCommand> RegisteredCommands
	{
		get;
	}

	public event AsyncEventHandler<CommandsNextExtension, CommandExecutionEventArgs> CommandExecuted;
	public event AsyncEventHandler<CommandsNextExtension, CommandErrorEventArgs> CommandErrored;
}

internal interface IDiscordMessage
{
	public ulong Id
	{
		get;
	}
	public IReadOnlyList<IDiscordReaction> Reactions
	{
		get;
	}
	public Task<IReadOnlyList<IDiscordUser>> GetReactionsAsync(IDiscordEmoji emoji);

	public Task DeleteReactionAsync(IDiscordEmoji emoji, IDiscordUser user);
	public Task CreateReactionAsync(IDiscordEmoji emoji);
	public Task ModifyAsync(IDiscordEmbed discordEmbed);
	public Task ModifyAsync(string value, IDiscordEmbed embed);
	public Task DeleteAsync();
	public Task DeleteReactionsEmojiAsync(IDiscordEmoji emoji);
	public Task<IDiscordMessage> RespondAsync(IDiscordEmbed embed);
	public Task<IDiscordMessage> RespondAsync(string content, IDiscordEmbed embed);

	public string Content
	{
		get;
	}
	public bool Pinned
	{
		get;
	}
	public DiscordMessage Inner
	{
		get;
	}
	public IDiscordUser Author
	{
		get;
	}
	public DateTimeOffset CreationTimestamp
	{
		get;
	}
	public IReadOnlyList<IDiscordEmbed> Embeds
	{
		get;
	}
	public IDiscordChannel Channel
	{
		get;
	}
	public DateTimeOffset Timestamp
	{
		get;
	}
	public IReadOnlyList<DiscordAttachment> Attachments
	{
		get;
	}
}
internal interface IDiscordReaction
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
internal interface IDiscordEmoji
{
	public DiscordEmoji Inner
	{
		get;
	}
	public string Name
	{
		get;
	}
	public string GetDiscordName();
	public string ToString();
}
internal interface IDiscordUser
{
	public DiscordUser Inner
	{
		get;
	}
	public ulong Id
	{
		get;
	}
	public bool IsBot
	{
		get;
	}
	public string Mention
	{
		get;
	}
	public string Discriminator
	{
		get;
	}
	public string Username
	{
		get;
	}

	public Task UnbanAsync(IDiscordGuild guild);
}
internal interface IDiscordChannel
{
	public Task<IReadOnlyList<IDiscordMessage>> GetMessagesAsync(int limit = 100);
	public Task<IDiscordInteractivityResult<IDiscordMessage>> GetNextMessageAsync(IDiscordUser user, TimeSpan timeoutOverride);
	public Task DeleteMessageAsync(IDiscordMessage message);
	public Task<IDiscordMessage> GetMessageAsync(ulong id);
	public Task<IDiscordMessage> SendMessageAsync(string content);
	public Task<IDiscordMessage> SendMessageAsync(IDiscordEmbed embed);
	public Task<IDiscordMessage> SendMessageAsync(string content, IDiscordEmbed embed);

	public ulong Id
	{
		get;
	}
	public string Name
	{
		get;
	}
	public DiscordChannel Inner
	{
		get;
	}
	public IDiscordGuild Guild
	{
		get;
	}
	public string Mention
	{
		get;
	}
	public bool IsPrivate
	{
		get;
	}
}
internal interface IDiscordCommand
{
	public string Name
	{
		get;
	}
	public IReadOnlyList<CommandOverload> Overloads
	{
		get;
	}

	public IReadOnlyList<string> Aliases
	{
		get;
	}
	public string Description
	{
		get;
	}
}

internal interface IDiscordCommandContext
{
	public ulong GuildId
	{
		get;
	}
	public IDiscordMember Member
	{
		get;
	}
	public IDiscordCommand Command
	{
		get;
	}
	public IDiscordChannel Channel
	{
		get;
	}
	public IDiscordMessage Message
	{
		get;
	}
	public IDiscordGuild Guild
	{
		get;
	}
	public IDiscordClient Client
	{
		get;
	}
	public IDiscordUser User
	{
		get;
	}
	public ICommandsNextExtension CommandsNext
	{
		get;
	}

	public Task SendUnauthorizedMessageAsync();

	public Task DeleteInProgressReactionAsync(IDiscordEmoji emoji);

	public Task AddErrorReactionAsync(IDiscordEmoji emoji);
}

internal interface IDiscordGuild
{
	public ulong Id
	{
		get;
	}
	public string Name
	{
		get;
	}
	public DiscordGuild Inner
	{
		get;
	}
	public IReadOnlyDictionary<ulong, IDiscordChannel> Channels
	{
		get;
	}
	public IReadOnlyDictionary<ulong, IDiscordRole> Roles
	{
		get;
	}
	public Task<IReadOnlyCollection<IDiscordMember>> GetAllMembersAsync();
	public Task<IDiscordMember> GetMemberAsync(ulong userId);
	public Task LeaveAsync();
	public IDiscordRole GetRole(ulong roleId);
	public IDiscordChannel GetChannel(ulong chatID);
	public Task UnbanMemberAsync(IDiscordUser user);
	public Task BanMemberAsync(IDiscordMember member);
}
internal interface IDiscordRole
{
	public ulong Id
	{
		get;
	}
	public string Name
	{
		get;
	}
	public string Mention
	{
		get;
	}
	public DiscordRole Inner
	{
		get;
	}
}
internal interface IDiscordMember
{
	public ulong Id
	{
		get;
	}
	public string DisplayName
	{
		get;
	}
	public string Username
	{
		get;
	}
	public string Discriminator
	{
		get;
	}
	public string Mention
	{
		get;
	}
	public IEnumerable<IDiscordRole> Roles
	{
		get;
	}
	public Task GrantRoleAsync(IDiscordRole role);
	public Task RevokeRoleAsync(IDiscordRole role);
	public Task<IDiscordMessage> SendMessageAsync(string message);
	public Task ModifyAsync(Action<MemberEditModel> action);
	public Task RemoveAsync(string reason);

	public DiscordMember Inner
	{
		get;
	}
	public bool IsBot
	{
		get;
	}
	public IDiscordGuild Guild
	{
		get;
	}
	public string AvatarUrl
	{
		get;
	}
	public DiscordPresence Presence
	{
		get;
	}
	public DateTimeOffset JoinedAt
	{
		get;
	}
	public bool? Verified
	{
		get;
	}
}

internal interface IDiscordEmbed
{
	public DiscordEmbed Inner
	{
		get;
	}
	public string Title
	{
		get;
	}
	public IEnumerable<DiscordEmbedField> Fields
	{
		get;
	}
	public DiscordEmbedThumbnail Thumbnail
	{
		get;
	}
	public string Description
	{
		get;
	}
}
