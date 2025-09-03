namespace NLBE_Bot.Models;

using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Net.Models;
using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

internal class DiscordClientWrapper(DiscordClient discordClient) : IDiscordClient
{
	private readonly DiscordClient _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));

	public DiscordClient Inner => _discordClient;
	public IReadOnlyDictionary<ulong, IDiscordGuild> Guilds => _discordClient.Guilds.ToDictionary(
		kvp => kvp.Key,
		kvp => (IDiscordGuild) new DiscordGuildWrapper(kvp.Value)
	);

	public Task ConnectAsync(DiscordActivity activity, UserStatus status)
	{
		return _discordClient.ConnectAsync(activity, status);
	}

	public Task DisconnectAsync()
	{
		return _discordClient.DisconnectAsync();
	}

	public ICommandsNextExtension UseCommandsNext(CommandsNextConfiguration config)
	{
		return new CommandsNextExtensionWrapper(_discordClient.UseCommandsNext(config));
	}
	public ICommandsNextExtension GetCommandsNext()
	{
		return new CommandsNextExtensionWrapper(_discordClient.GetCommandsNext());
	}
	public async Task<IDiscordUser> GetUserAsync(ulong userId)
	{
		return new DiscordUserWrapper(await _discordClient.GetUserAsync(userId));
	}
	public async Task<IDiscordGuild> GetGuildAsync(ulong guildId)
	{
		return new DiscordGuildWrapper(await _discordClient.GetGuildAsync(guildId));
	}
	public IDiscordInteractivityExtension GetInteractivity()
	{
		return new DiscordInteractivityExtensionWrapper(_discordClient.GetInteractivity());
	}
	public async Task<IDiscordMessage> SendMessageAsync(IDiscordChannel channel, string content, IDiscordEmbed? embed = null)
	{
		return new DiscordMessageWrapper(await _discordClient.SendMessageAsync(channel.Inner, content, embed?.Inner));
	}

	public event AsyncEventHandler<DiscordClient, ReadyEventArgs> Ready
	{
		add => _discordClient.Ready += value; remove => _discordClient.Ready -= value;
	}
	public event AsyncEventHandler<DiscordClient, HeartbeatEventArgs> Heartbeated
	{
		add => _discordClient.Heartbeated += value; remove => _discordClient.Heartbeated -= value;
	}
	public event AsyncEventHandler<DiscordClient, MessageCreateEventArgs> MessageCreated
	{
		add => _discordClient.MessageCreated += value; remove => _discordClient.MessageCreated -= value;
	}
	public event AsyncEventHandler<DiscordClient, MessageDeleteEventArgs> MessageDeleted
	{
		add => _discordClient.MessageDeleted += value; remove => _discordClient.MessageDeleted -= value;
	}
	public event AsyncEventHandler<DiscordClient, MessageReactionAddEventArgs> MessageReactionAdded
	{
		add => _discordClient.MessageReactionAdded += value; remove => _discordClient.MessageReactionAdded -= value;
	}
	public event AsyncEventHandler<DiscordClient, MessageReactionRemoveEventArgs> MessageReactionRemoved
	{
		add => _discordClient.MessageReactionRemoved += value; remove => _discordClient.MessageReactionRemoved -= value;
	}
	public event AsyncEventHandler<DiscordClient, GuildMemberAddEventArgs> GuildMemberAdded
	{
		add => _discordClient.GuildMemberAdded += value; remove => _discordClient.GuildMemberAdded -= value;
	}
	public event AsyncEventHandler<DiscordClient, GuildMemberUpdateEventArgs> GuildMemberUpdated
	{
		add => _discordClient.GuildMemberUpdated += value; remove => _discordClient.GuildMemberUpdated -= value;
	}
	public event AsyncEventHandler<DiscordClient, GuildMemberRemoveEventArgs> GuildMemberRemoved
	{
		add => _discordClient.GuildMemberRemoved += value; remove => _discordClient.GuildMemberRemoved -= value;
	}
	public event AsyncEventHandler<DiscordClient, ClientErrorEventArgs> ClientErrored
	{
		add => _discordClient.ClientErrored += value; remove => _discordClient.ClientErrored -= value;
	}
	public event AsyncEventHandler<DiscordClient, SocketCloseEventArgs> SocketClosed
	{
		add => _discordClient.SocketClosed += value; remove => _discordClient.SocketClosed -= value;
	}
}

internal class DiscordInteractivityExtensionWrapper(InteractivityExtension interactivity) : IDiscordInteractivityExtension
{
	private readonly InteractivityExtension _interactivity = interactivity ?? throw new ArgumentNullException(nameof(interactivity));

	public InteractivityExtension Inner => _interactivity;

	public Task<IDiscordInteractivityResult<IDiscordMessage>> WaitForMessageAsync(Func<IDiscordMessage, bool> value)
	{
		return _interactivity.WaitForMessageAsync(
			msg => value(new DiscordMessageWrapper(msg))
		).ContinueWith(t => (IDiscordInteractivityResult<IDiscordMessage>) new DiscordInteractivityResultWrapper<IDiscordMessage>(t.Result));
	}
}

internal class DiscordInteractivityResultWrapper<T>(InteractivityResult<DiscordMessage> result) : IDiscordInteractivityResult<T>
{
	private readonly InteractivityResult<DiscordMessage> _result = result.Equals(default)
																	? throw new ArgumentException("Result is default", nameof(result))
																	: result;

	public bool TimedOut => _result.TimedOut;

	public T Result => (T) (IDiscordMessage) new DiscordMessageWrapper(_result.Result);
}

internal class CommandsNextExtensionWrapper(CommandsNextExtension command) : ICommandsNextExtension
{
	private readonly CommandsNextExtension _command = command ?? throw new ArgumentNullException(nameof(command));

	public void RegisterCommands<T>() where T : BaseCommandModule
	{
		_command.RegisterCommands<T>();
	}

	public IReadOnlyDictionary<string, IDiscordCommand> RegisteredCommands =>
		_command.RegisteredCommands.ToDictionary(
			kvp => kvp.Key,
			kvp => (IDiscordCommand) new DiscordCommandWrapper(kvp.Value)
		);

	public Command? FindCommand(string commandName, out string? rawArguments)
	{
		return _command.FindCommand(commandName, out rawArguments);
	}

	public CommandContext CreateContext(IDiscordMessage message, string prefix, Command command, string args)
	{
		return _command.CreateContext(message.Inner, prefix, command, args);
	}

	public Task ExecuteCommandAsync(CommandContext ctx)
	{
		return _command.ExecuteCommandAsync(ctx);
	}

	public event AsyncEventHandler<CommandsNextExtension, CommandExecutionEventArgs> CommandExecuted
	{
		add => _command.CommandExecuted += value; remove => _command.CommandExecuted -= value;
	}
	public event AsyncEventHandler<CommandsNextExtension, CommandErrorEventArgs> CommandErrored
	{
		add => _command.CommandErrored += value; remove => _command.CommandErrored -= value;
	}
}

internal class DiscordMessageWrapper(DiscordMessage message) : IDiscordMessage
{
	private readonly DiscordMessage _message = message ?? throw new ArgumentNullException(nameof(message));

	public DiscordMessage Inner => _message;

	public string Content => _message.Content;

	public ulong Id => _message.Id;

	public bool Pinned => _message.Pinned;

	public IDiscordUser Author => new DiscordUserWrapper(_message.Author);

	public DateTimeOffset Timestamp => _message.Timestamp;

	public IReadOnlyList<IDiscordAttachment> Attachments =>
		_message.Attachments.Select(r => (IDiscordAttachment) new DiscordAttachmentWrapper(r)).ToList().AsReadOnly();

	public IReadOnlyList<IDiscordReaction> Reactions =>
		_message.Reactions.Select(r => (IDiscordReaction) new DiscordReactionWrapper(r)).ToList().AsReadOnly();

	public DateTimeOffset CreationTimestamp => _message.CreationTimestamp;

	public IReadOnlyList<IDiscordEmbed> Embeds =>
		_message.Embeds.Select(e => (IDiscordEmbed) new DiscordEmbedWrapper(e)).ToList().AsReadOnly();

	public IDiscordChannel Channel => new DiscordChannelWrapper(_message.Channel);

	public Task<IReadOnlyList<IDiscordUser>> GetReactionsAsync(IDiscordEmoji emoji)
	{
		return _message.GetReactionsAsync(((DiscordEmojiWrapper) emoji).Inner)
			.ContinueWith(t => (IReadOnlyList<IDiscordUser>) [.. t.Result.Select(u => new DiscordUserWrapper(u))]);
	}

	public Task DeleteReactionAsync(IDiscordEmoji emoji, IDiscordUser user)
	{
		return _message.DeleteReactionAsync(emoji.Inner, user.Inner);
	}

	public Task CreateReactionAsync(IDiscordEmoji emoji)
	{
		return _message.CreateReactionAsync(emoji.Inner);
	}

	public Task ModifyAsync(IDiscordEmbed discordEmbed)
	{
		return _message.ModifyAsync(discordEmbed.Inner);
	}

	public Task ModifyAsync(string value, IDiscordEmbed embed)
	{
		return _message.ModifyAsync(value, embed.Inner);
	}

	public async Task<IDiscordMessage> RespondAsync(IDiscordEmbed embed)
	{
		return new DiscordMessageWrapper(await _message.RespondAsync(embed.Inner));
	}

	public async Task<IDiscordMessage> RespondAsync(string content, IDiscordEmbed embed)
	{
		return new DiscordMessageWrapper(await _message.RespondAsync(content, embed.Inner));
	}

	public Task DeleteAsync()
	{
		return _message.DeleteAsync();
	}

	public Task DeleteReactionsEmojiAsync(IDiscordEmoji emoji)
	{
		return _message.DeleteReactionsEmojiAsync(emoji.Inner);
	}
}

internal class DiscordAttachmentWrapper(DiscordAttachment attachment) : IDiscordAttachment
{
	private readonly DiscordAttachment _attachment = attachment ?? throw new ArgumentNullException(nameof(attachment));

	public ulong Id => _attachment.Id;

	public string Url => _attachment.Url;

	public string FileName => _attachment.FileName;

	public DiscordAttachment Inner => _attachment;

	public int FileSize => _attachment.FileSize;
}

internal class DiscordReactionWrapper(DiscordReaction reaction) : IDiscordReaction
{
	private readonly DiscordReaction _reaction = reaction ?? throw new ArgumentNullException(nameof(reaction));

	public IDiscordEmoji Emoji => new DiscordEmojiWrapper(_reaction.Emoji);

	public DiscordReaction Inner
	{
		get;
	} = reaction;
}

internal class DiscordEmojiWrapper(DiscordEmoji emoji) : IDiscordEmoji
{
	private readonly DiscordEmoji _emoji = emoji ?? throw new ArgumentNullException(nameof(emoji));

	public DiscordEmoji Inner => _emoji;

	public string Name => _emoji.Name;

	public string GetDiscordName()
	{
		return Inner.GetDiscordName();
	}

	public override string ToString()
	{
		return Inner.ToString();
	}
}

internal class DiscordUserWrapper(DiscordUser user) : IDiscordUser
{
	private readonly DiscordUser _user = user ?? throw new ArgumentNullException(nameof(user));

	public DiscordUser Inner => _user;
	public ulong Id => _user.Id;
	public bool IsBot => _user.IsBot;
	public string Mention => _user.Mention;
	public string Discriminator => _user.Discriminator;
	public string Username => _user.Username;

	public Task UnbanAsync(IDiscordGuild guild)
	{
		return _user.UnbanAsync(guild.Inner);
	}
}

internal class DiscordChannelWrapper(DiscordChannel channel) : IDiscordChannel
{
	private readonly DiscordChannel _channel = channel ?? throw new ArgumentNullException(nameof(channel));
	public string Mention => _channel.Mention;

	public Task<IReadOnlyList<IDiscordMessage>> GetMessagesAsync(int limit = 100)
	{
		return _channel.GetMessagesAsync(limit)
			.ContinueWith(t => (IReadOnlyList<IDiscordMessage>) [.. t.Result.Select(u => new DiscordMessageWrapper(u))]);
	}
	public Task<IDiscordInteractivityResult<IDiscordMessage>> GetNextMessageAsync(IDiscordUser user, TimeSpan timeoutOverride)
	{
		return _channel.GetNextMessageAsync(user.Inner, timeoutOverride)
			.ContinueWith(t => (IDiscordInteractivityResult<IDiscordMessage>) new DiscordInteractivityResultWrapper<IDiscordMessage>(t.Result));
	}

	public Task DeleteMessageAsync(IDiscordMessage message)
	{
		return _channel.DeleteMessageAsync(message.Inner);
	}

	public async Task<IDiscordMessage> GetMessageAsync(ulong id)
	{
		return new DiscordMessageWrapper(await _channel.GetMessageAsync(id));
	}

	public async Task<IDiscordMessage> SendMessageAsync(string content)
	{
		return new DiscordMessageWrapper(await _channel.SendMessageAsync(content));
	}

	public async Task<IDiscordMessage> SendMessageAsync(IDiscordEmbed embed)
	{
		return new DiscordMessageWrapper(await _channel.SendMessageAsync(embed.Inner));
	}

	public async Task<IDiscordMessage> SendMessageAsync(string content, IDiscordEmbed embed)
	{
		return new DiscordMessageWrapper(await _channel.SendMessageAsync(content, embed.Inner));
	}
	public ulong Id => _channel.Id;

	public string Name => _channel.Name;

	public IDiscordGuild Guild => new DiscordGuildWrapper(_channel.Guild);

	public DiscordChannel Inner => _channel;

	public bool IsPrivate => _channel.IsPrivate;
}

internal class DiscordCommandWrapper(Command command) : IDiscordCommand
{
	private readonly Command _command = command ?? throw new ArgumentNullException(nameof(command));

	public string Name => _command.Name;

	public IReadOnlyList<CommandOverload> Overloads => _command.Overloads;

	public IReadOnlyList<string> Aliases => _command.Aliases;

	public string Description => _command.Description;
}

internal class CommandContextWrapper(CommandContext context) : IDiscordCommandContext
{
	private readonly CommandContext _context = context ?? throw new ArgumentNullException(nameof(context));

	public ulong GuildId => _context.Guild.Id;

	public IDiscordMember Member => new DiscordMemberWrapper(_context.Member);

	public IDiscordCommand Command => new DiscordCommandWrapper(_context.Command);

	public IDiscordChannel Channel => new DiscordChannelWrapper(_context.Channel);

	public IDiscordMessage Message => new DiscordMessageWrapper(_context.Message);

	public IDiscordGuild Guild => new DiscordGuildWrapper(_context.Guild);

	public IDiscordClient Client => new DiscordClientWrapper(_context.Client);

	public IDiscordUser User => new DiscordUserWrapper(_context.User);

	public ICommandsNextExtension CommandsNext => new CommandsNextExtensionWrapper(_context.CommandsNext);

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

internal class DiscordGuildWrapper(DiscordGuild guild) : IDiscordGuild
{
	private readonly DiscordGuild _guild = guild ?? throw new ArgumentNullException(nameof(guild));

	public ulong Id => _guild.Id;
	public string Name => _guild.Name;

	public DiscordGuild Inner => _guild;

	public IReadOnlyDictionary<ulong, IDiscordChannel> Channels =>
		_guild.Channels.ToDictionary(
			kvp => kvp.Key,
			kvp => (IDiscordChannel) new DiscordChannelWrapper(kvp.Value)
		);

	public IReadOnlyDictionary<ulong, IDiscordRole> Roles =>
		_guild.Roles.ToDictionary(
			kvp => kvp.Key,
			kvp => (IDiscordRole) new DiscordRoleWrapper(kvp.Value)
		);

	public async Task<IReadOnlyCollection<IDiscordMember>> GetAllMembersAsync()
	{
		IReadOnlyCollection<DiscordMember> members = await _guild.GetAllMembersAsync();
		return [.. members.Select(m => (IDiscordMember) new DiscordMemberWrapper(m))];
	}

	public async Task<IDiscordMember?> GetMemberAsync(ulong userId)
	{
		DiscordMember member = await _guild.GetMemberAsync(userId);
		return member != null ? new DiscordMemberWrapper(member) : null;
	}

	public Task LeaveAsync()
	{
		return _guild.LeaveAsync();
	}

	public IDiscordRole? GetRole(ulong roleId)
	{
		return _guild.Roles.TryGetValue(roleId, out DiscordRole? role) ? new DiscordRoleWrapper(role) : null;
	}

	public IDiscordChannel? GetChannel(ulong id)
	{
		DiscordChannel channel = _guild.GetChannel(id);
		return channel != null ? new DiscordChannelWrapper(channel) : null;
	}

	public Task UnbanMemberAsync(IDiscordUser user)
	{
		return _guild.UnbanMemberAsync(user.Inner);
	}

	public Task BanMemberAsync(IDiscordMember member)
	{
		return _guild.BanMemberAsync(member.Inner);
	}
}

internal class DiscordRoleWrapper(DiscordRole role) : IDiscordRole
{
	private readonly DiscordRole _role = role ?? throw new ArgumentNullException(nameof(role));

	public ulong Id => _role.Id;
	public string Name => _role.Name;
	public string Mention => _role.Mention;
	public DiscordRole Inner => _role;
}

internal class DiscordMemberWrapper(DiscordMember member) : IDiscordMember
{
	private readonly DiscordMember _member = member ?? throw new ArgumentNullException(nameof(member));

	public ulong Id => _member.Id;
	public string DisplayName => _member.DisplayName;
	public string Username => _member.Username;
	public string Discriminator => _member.Discriminator;
	public string Mention => _member.Mention;
	public IEnumerable<IDiscordRole> Roles => _member.Roles.Select(r => new DiscordRoleWrapper(r));
	public DiscordMember Inner => _member;

	public bool IsBot => _member.IsBot;

	public IDiscordGuild Guild => new DiscordGuildWrapper(_member.Guild);

	public string AvatarUrl => _member.AvatarUrl;

	public DiscordPresence Presence => _member.Presence;

	public DateTimeOffset JoinedAt => _member.JoinedAt;

	public bool? Verified => _member.Verified;

	public Task GrantRoleAsync(IDiscordRole role)
	{
		return _member.GrantRoleAsync(role.Inner);
	}

	public Task RevokeRoleAsync(IDiscordRole role)
	{
		return _member.RevokeRoleAsync(role.Inner);
	}

	public async Task<IDiscordMessage> SendMessageAsync(string message)
	{
		return new DiscordMessageWrapper(await _member.SendMessageAsync(message));
	}
	public Task ModifyAsync(Action<MemberEditModel> action)
	{
		return _member.ModifyAsync(action);
	}
	public Task RemoveAsync(string reason)
	{
		return _member.RemoveAsync(reason);
	}
}

internal class DiscordEmbedWrapper(DiscordEmbed embed) : IDiscordEmbed
{
	private readonly DiscordEmbed _embed = embed ?? throw new ArgumentNullException(nameof(embed));

	public DiscordEmbed Inner => _embed;

	public string Title => _embed.Title;

	public string Description => _embed.Description;

	public DiscordEmbedFooter Footer => _embed.Footer;

	public DiscordEmbedImage Image => _embed.Image;

	public IReadOnlyList<DiscordEmbedField> Fields => _embed.Fields;

	public DiscordEmbedThumbnail Thumbnail => _embed.Thumbnail;
}
