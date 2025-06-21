namespace NLBE_Bot.Models;

using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using NLBE_Bot.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DiscordClientWrapper(DiscordClient client) : IDiscordClient
{
	private readonly DiscordClient _inner = client;

	public DiscordClient Inner => _inner;
	public IReadOnlyDictionary<ulong, IDiscordGuild> Guilds => _inner.Guilds.ToDictionary(
		kvp => kvp.Key,
		kvp => (IDiscordGuild) new DiscordGuildWrapper(kvp.Value)
	);

	public Task ConnectAsync(DiscordActivity activity, UserStatus status)
	{
		return _inner.ConnectAsync(activity, status);
	}
	public ICommandsNextExtension UseCommandsNext(CommandsNextConfiguration config)
	{
		return new CommandsNextExtensionWrapper(_inner.UseCommandsNext(config));
	}
	public ICommandsNextExtension GetCommandsNext()
	{
		return new CommandsNextExtensionWrapper(_inner.GetCommandsNext());
	}
	public Task<DiscordUser> GetUserAsync(ulong userId)
	{
		return _inner.GetUserAsync(userId);
	}
	public Task<DiscordGuild> GetGuildAsync(ulong guildId)
	{
		return _inner.GetGuildAsync(guildId);
	}
	public InteractivityExtension GetInteractivity()
	{
		return _inner.GetInteractivity();
	}
	public Task<DiscordMessage> SendMessageAsync(DiscordChannel channel, string content, DiscordEmbed embed = null)
	{
		return _inner.SendMessageAsync(channel, content, embed);
	}
	public event AsyncEventHandler<DiscordClient, ReadyEventArgs> Ready
	{
		add => _inner.Ready += value; remove => _inner.Ready -= value;
	}
	public event AsyncEventHandler<DiscordClient, HeartbeatEventArgs> Heartbeated
	{
		add => _inner.Heartbeated += value; remove => _inner.Heartbeated -= value;
	}
	public event AsyncEventHandler<DiscordClient, MessageCreateEventArgs> MessageCreated
	{
		add => _inner.MessageCreated += value; remove => _inner.MessageCreated -= value;
	}
	public event AsyncEventHandler<DiscordClient, MessageDeleteEventArgs> MessageDeleted
	{
		add => _inner.MessageDeleted += value; remove => _inner.MessageDeleted -= value;
	}
	public event AsyncEventHandler<DiscordClient, MessageReactionAddEventArgs> MessageReactionAdded
	{
		add => _inner.MessageReactionAdded += value; remove => _inner.MessageReactionAdded -= value;
	}
	public event AsyncEventHandler<DiscordClient, MessageReactionRemoveEventArgs> MessageReactionRemoved
	{
		add => _inner.MessageReactionRemoved += value; remove => _inner.MessageReactionRemoved -= value;
	}
	public event AsyncEventHandler<DiscordClient, GuildMemberAddEventArgs> GuildMemberAdded
	{
		add => _inner.GuildMemberAdded += value; remove => _inner.GuildMemberAdded -= value;
	}
	public event AsyncEventHandler<DiscordClient, GuildMemberUpdateEventArgs> GuildMemberUpdated
	{
		add => _inner.GuildMemberUpdated += value; remove => _inner.GuildMemberUpdated -= value;
	}
	public event AsyncEventHandler<DiscordClient, GuildMemberRemoveEventArgs> GuildMemberRemoved
	{
		add => _inner.GuildMemberRemoved += value; remove => _inner.GuildMemberRemoved -= value;
	}
}
public class CommandsNextExtensionWrapper : ICommandsNextExtension
{
	private readonly CommandsNextExtension _inner;
	public CommandsNextExtensionWrapper(CommandsNextExtension inner)
	{
		_inner = inner;
	}
	public void RegisterCommands<T>() where T : BaseCommandModule
	{
		_inner.RegisterCommands<T>();
	}
	public event AsyncEventHandler<CommandsNextExtension, CommandExecutionEventArgs> CommandExecuted
	{
		add => _inner.CommandExecuted += value; remove => _inner.CommandExecuted -= value;
	}
	public event AsyncEventHandler<CommandsNextExtension, CommandErrorEventArgs> CommandErrored
	{
		add => _inner.CommandErrored += value; remove => _inner.CommandErrored -= value;
	}
}
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

public static class DiscordMessageExtensions
{
	public static Task<DiscordMessage> RespondAsync(this IDiscordMessage message, string content, DiscordEmbed embed)
	{
		return message.Inner.RespondAsync(content, embed: embed);
	}
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

	public string GetDiscordName()
	{
		return Inner.GetDiscordName();
	}

	public override string ToString()
	{
		return Inner.ToString();
	}
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

public class CommandWrapper(Command command) : ICommand
{
	private readonly Command _command = command;

	public string Name => _command.Name;
}

public class CommandContextWrapper(CommandContext context) : ICommandContext
{
	private readonly CommandContext _context = context;

	public ulong GuildId => _context.Guild.Id;

	public Task SendUnauthorizedMessageAsync()
	{
		return _context.Channel.SendMessageAsync("**De bot heeft hier geen rechten voor!**");
	}

	public Task DeleteInProgressReactionAsync(IDiscordEmoji emoji)
	{
		return _context.Message.DeleteReactionsEmojiAsync(emoji.Inner);
	}

	public Task AddErrorReactionAsync(IDiscordEmoji emoji)
	{
		return _context.Message.CreateReactionAsync(emoji.Inner);
	}
}

public class DiscordGuildWrapper(DiscordGuild guild) : IDiscordGuild
{
	private readonly DiscordGuild _guild = guild;

	public ulong Id => _guild.Id;

	public DiscordGuild Inner => _guild;

	public IReadOnlyDictionary<ulong, DiscordChannel> Channels => _guild.Channels;

	public IReadOnlyDictionary<ulong, DiscordRole> Roles => _guild.Roles;

	public Task<IReadOnlyCollection<DiscordMember>> GetAllMembersAsync()
	{
		return _guild.GetAllMembersAsync();
	}

	public Task<DiscordMember> GetMemberAsync(ulong userId)
	{
		return _guild.GetMemberAsync(userId);
	}

	public Task LeaveAsync()
	{
		return _guild.LeaveAsync();
	}
}
