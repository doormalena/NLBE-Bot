namespace NLBE_Bot.Tests.EventHandlers;

using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.EventHandlers;
using NLBE_Bot.Interfaces;
using NSubstitute;

[TestClass]
public class MessageEventHandlerTests
{
	private IDiscordClient _discordClientMock = null!;
	private ILogger<MessageEventHandler> _loggerMock = null!;
	private IOptions<BotOptions> _options = null!;
	private IUserService _userServiceMock = null!;
	private IDiscordMessageUtils _discordMessageUtilsMock = null!;
	private IWeeklyEventService _weeklyEventServiceMock = null!;
	private IMapService _mapServiceMock = null!;
	private IReplayService _replayServiceMock = null!;
	private ITournamentService _tournamentServiceMock = null!;
	private IHallOfFameService _hallOfFameServiceMock = null!;
	private IMessageService _messageServiceMock = null!;
	private IDiscordGuild _guildMock = null!;
	private MessageEventHandler _handler = null!;

	private IDiscordChannel _rulesChannelMock = null!;
	private IDiscordChannel _generalChannelMock = null!;
	private IDiscordChannel _tournamentSignUpChannelMock = null!;

	#region TestInitialize

	[TestInitialize]
	public void Setup()
	{
		BotOptions botOptions = new()
		{
			ChannelIds = new()
			{
				Rules = 999UL,
				General = 888UL,
				TournamentSignUp = 777UL,
				WeeklyEvent = 666UL,
				MasteryReplays = 555UL,
				ReplayResults = 444UL,
				BotTest = 333UL
			},
			RoleIds = new()
			{
				MustReadRules = 111UL,
				Members = 222UL
			},
			ServerId = 12345
		};

		_options = Options.Create(botOptions);

		_rulesChannelMock = Substitute.For<IDiscordChannel>();
		_rulesChannelMock.Id.Returns(botOptions.ChannelIds.Rules);

		_generalChannelMock = Substitute.For<IDiscordChannel>();
		_generalChannelMock.Id.Returns(botOptions.ChannelIds.General);

		_tournamentSignUpChannelMock = Substitute.For<IDiscordChannel>();
		_tournamentSignUpChannelMock.Id.Returns(botOptions.ChannelIds.TournamentSignUp);

		_guildMock = Substitute.For<IDiscordGuild>();
		_guildMock.Id.Returns(botOptions.ServerId);

		_guildMock.GetChannel(botOptions.ChannelIds.Rules).Returns(_rulesChannelMock);
		_guildMock.GetChannel(botOptions.ChannelIds.General).Returns(_generalChannelMock);
		_guildMock.GetChannel(botOptions.ChannelIds.TournamentSignUp).Returns(_tournamentSignUpChannelMock);

		_discordClientMock = Substitute.For<IDiscordClient>();
		_discordClientMock.GetGuildAsync(botOptions.ServerId).Returns(Task.FromResult(_guildMock));

		_loggerMock = Substitute.For<ILogger<MessageEventHandler>>();
		_userServiceMock = Substitute.For<IUserService>();
		_discordMessageUtilsMock = Substitute.For<IDiscordMessageUtils>();
		_weeklyEventServiceMock = Substitute.For<IWeeklyEventService>();
		_mapServiceMock = Substitute.For<IMapService>();
		_replayServiceMock = Substitute.For<IReplayService>();
		_tournamentServiceMock = Substitute.For<ITournamentService>();
		_hallOfFameServiceMock = Substitute.For<IHallOfFameService>();
		_messageServiceMock = Substitute.For<IMessageService>();

		_handler = new MessageEventHandler(
			_options,
			_loggerMock,
			_userServiceMock,
			_discordMessageUtilsMock,
			_weeklyEventServiceMock,
			_mapServiceMock,
			_replayServiceMock,
			_tournamentServiceMock,
			_hallOfFameServiceMock,
			_messageServiceMock
		);
	}

	#endregion

	#region Register

	[TestMethod]
	public void Register_RegistersAllEvents()
	{
		// Act.
		_handler.Register(_discordClientMock, Substitute.For<IBotState>());

		// Assert.
		_discordClientMock.Received(1).MessageCreated += Arg.Any<AsyncEventHandler<DiscordClient, MessageCreateEventArgs>>();
		_discordClientMock.Received(1).MessageDeleted += Arg.Any<AsyncEventHandler<DiscordClient, MessageDeleteEventArgs>>();
		_discordClientMock.Received(1).MessageReactionAdded += Arg.Any<AsyncEventHandler<DiscordClient, MessageReactionAddEventArgs>>();
		_discordClientMock.Received(1).MessageReactionRemoved += Arg.Any<AsyncEventHandler<DiscordClient, MessageReactionRemoveEventArgs>>();
	}

	#endregion

	#region HandleMessageCreated

	[TestMethod]
	public async Task HandleMessageCreated_IgnoresBotInPublicChannel()
	{
		// Arrange.
		_handler.Register(_discordClientMock, Substitute.For<IBotState>());
		IDiscordUser botUser = Substitute.For<IDiscordUser>();
		botUser.IsBot.Returns(true);

		IDiscordChannel publicChannel = Substitute.For<IDiscordChannel>();
		publicChannel.IsPrivate.Returns(false);

		IDiscordMessage msg = Substitute.For<IDiscordMessage>();
		msg.Attachments.Returns([]);

		_guildMock.GetChannel(_options.Value.ChannelIds.WeeklyEvent).Returns(Substitute.For<IDiscordChannel>());
		_guildMock.GetChannel(_options.Value.ChannelIds.MasteryReplays).Returns(Substitute.For<IDiscordChannel>());
		_guildMock.GetChannel(_options.Value.ChannelIds.ReplayResults).Returns(Substitute.For<IDiscordChannel>());
		_guildMock.GetChannel(_options.Value.ChannelIds.BotTest).Returns(Substitute.For<IDiscordChannel>());

		// Act.
		await _handler.HandleMessageCreated(_guildMock, publicChannel, msg, botUser);

		// Assert.
		await _replayServiceMock.DidNotReceive().GetReplayInfo(Arg.Any<string>(), Arg.Any<IDiscordAttachment>());
		await _weeklyEventServiceMock.DidNotReceive().CreateNewWeeklyEvent(Arg.Any<string>(), Arg.Any<IDiscordChannel>());
	}

	[TestMethod]
	public async Task HandleMessageCreated_ReturnsEarly_WhenRequiredChannelMissing()
	{
		// Arrange.
		_handler.Register(_discordClientMock, Substitute.For<IBotState>());
		_guildMock.GetChannel(_options.Value.ChannelIds.WeeklyEvent).Returns((IDiscordChannel?) null);

		IDiscordUser user = Substitute.For<IDiscordUser>();
		user.IsBot.Returns(false);

		IDiscordChannel channel = Substitute.For<IDiscordChannel>();
		IDiscordMessage msg = Substitute.For<IDiscordMessage>();

		// Act.
		await _handler.HandleMessageCreated(_guildMock, channel, msg, user);

		// Assert.
		await _replayServiceMock.DidNotReceive().GetReplayInfo(Arg.Any<string>(), Arg.Any<IDiscordAttachment>());
	}

	#endregion

	#region HandleMessageDeleted

	[TestMethod]
	public async Task HandleMessageDeleted_IgnoresWhenNotTournamentChannel()
	{
		// Arrange.
		_handler.Register(_discordClientMock, Substitute.For<IBotState>());
		IDiscordMessage msg = Substitute.For<IDiscordMessage>();
		IDiscordChannel otherChannel = Substitute.For<IDiscordChannel>();
		otherChannel.Id.Returns(123UL);

		// Act.
		await _handler.HandleMessageDeleted(msg, _guildMock, otherChannel);

		// Assert.
		await _tournamentServiceMock.DidNotReceive().GenerateLogMessage(
			Arg.Any<IDiscordMessage>(),
			Arg.Any<IDiscordChannel>(),
			Arg.Any<ulong>(),
			Arg.Any<string>());
	}

	#endregion

	#region HandleMessageReactionAdded

	[TestMethod]
	public async Task HandleMessageReactionAdded_ProcessesRulesReadEmoji()
	{
		// Arrange.
		ulong reactingUserId = 555UL;

		IDiscordRole membersRole = Substitute.For<IDiscordRole>();
		membersRole.Id.Returns(_options.Value.RoleIds.Members);
		_guildMock.GetRole(_options.Value.RoleIds.Members).Returns(membersRole);

		IDiscordEmoji emoji = Substitute.For<IDiscordEmoji>();
		emoji.GetDiscordName().Returns(":ok:");
		emoji.Name.Returns("ok");
		_discordMessageUtilsMock.GetDiscordEmoji("ok").Returns(emoji);

		IDiscordMessage message = Substitute.For<IDiscordMessage>();
		IDiscordUser reactingUser = Substitute.For<IDiscordUser>();
		reactingUser.Id.Returns(reactingUserId);
		reactingUser.IsBot.Returns(false);

		message.GetReactionsAsync(emoji).Returns([reactingUser]);

		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.Id.Returns(reactingUserId);
		member.DisplayName.Returns("[NLBE] PlayerName");
		member.Username.Returns("PlayerName");

		IDiscordRole mustReadRulesRole = Substitute.For<IDiscordRole>();
		mustReadRulesRole.Id.Returns(_options.Value.RoleIds.MustReadRules);
		member.Roles.Returns([mustReadRulesRole]);

		_guildMock.GetMemberAsync(reactingUserId).Returns(member);

		IDiscordUser addingUser = Substitute.For<IDiscordUser>();
		addingUser.IsBot.Returns(false);
		addingUser.Mention.Returns("@PlayerName");

		_handler.Register(_discordClientMock, Substitute.For<IBotState>());

		// Act.
		await _handler.HandleMessageReactionAdded(message, _guildMock, _rulesChannelMock, addingUser, emoji);

		// Assert.
		await message.Received(1).DeleteReactionAsync(emoji, reactingUser);
		await member.Received(1).RevokeRoleAsync(mustReadRulesRole);
		await _userServiceMock.Received(1).ChangeMemberNickname(member, "[] PlayerName");
		await member.Received(1).GrantRoleAsync(membersRole);
		await _generalChannelMock.Received(1).SendMessageAsync("@PlayerName, welkom op de NLBE discord server. Good luck, have fun!");
	}

	#endregion

	#region HandleMessageReactionRemoved

	[TestMethod]
	public async Task HandleMessageReactionRemoved_ReAddsReaction_WhenNoUsersRemain()
	{
		// Arrange
		_handler.Register(_discordClientMock, Substitute.For<IBotState>());

		IDiscordEmoji emoji = Substitute.For<IDiscordEmoji>();
		IDiscordMessage msg = Substitute.For<IDiscordMessage>();
		msg.Id.Returns(123UL);
		msg.GetReactionsAsync(emoji).Returns([]);

		// Message in TournamentSignUp channel must belong to bot
		IDiscordMessage messageTmp = Substitute.For<IDiscordMessage>();
		IDiscordUser botUser = Substitute.For<IDiscordUser>();
		botUser.Id.Returns(Constants.NLBE_BOT);
		messageTmp.Author.Returns(botUser);

		_tournamentSignUpChannelMock.GetMessageAsync(Arg.Any<ulong>())
			.Returns(Task.FromResult(messageTmp));

		// Log channel must return at least one message
		IDiscordChannel logChannel = Substitute.For<IDiscordChannel>();
		IDiscordMessage logMsg = Substitute.For<IDiscordMessage>();
		logMsg.Content.Returns("2024|user|display|emoji");
		logMsg.CreationTimestamp.Returns(DateTimeOffset.Now);

		logChannel.GetMessagesAsync(100).Returns([logMsg]);

		_guildMock.GetChannel(_options.Value.ChannelIds.TournamentSignUp).Returns(_tournamentSignUpChannelMock);
		_guildMock.GetChannel(_options.Value.ChannelIds.Log).Returns(logChannel);

		// SortMessages must return a valid dictionary
		_discordMessageUtilsMock.SortMessages(Arg.Any<IReadOnlyList<IDiscordMessage>>())
			.Returns(new Dictionary<DateTime, List<IDiscordMessage>>
			{
			{ DateTime.Now, new List<IDiscordMessage> { logMsg } }
			});

		// UserService must return a valid member
		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.DisplayName.Returns("display");
		_userServiceMock.GetDiscordMember(_guildMock, Arg.Any<ulong>())
			.Returns(member);

		// Emoji string conversion must not be null
		_discordMessageUtilsMock.GetEmojiAsString(Arg.Any<string>())
			.Returns("emoji");

		IDiscordUser user = Substitute.For<IDiscordUser>();
		user.Id.Returns(999UL);

		// Act
		await _handler.HandleMessageReactionRemoved(msg, _guildMock, _tournamentSignUpChannelMock, user, emoji);

		// Assert
		await msg.Received(1).CreateReactionAsync(emoji);
	}

	#endregion
}
