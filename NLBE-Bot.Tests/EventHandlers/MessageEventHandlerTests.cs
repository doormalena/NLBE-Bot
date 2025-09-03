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
	private IDiscordClient? _discordClientMock;
	private ILogger<MessageEventHandler>? _loggerMock;
	private IOptions<BotOptions>? _options;
	private IUserService? _userServiceMock;
	private IDiscordMessageUtils? _discordMessageUtilsMock;
	private IWeeklyEventService? _weeklyEventServiceMock;
	private IMapService? _mapServiceMock;
	private IReplayService? _replayServiceMock;
	private ITournamentService? _tournamentServiceMock;
	private IHallOfFameService? _hallOfFameServiceMock;
	private IMessageService? _messageServiceMock;
	private IDiscordGuild? _guildMock;
	private IDiscordChannel? _rulesChannelMock;
	private IDiscordChannel? _generalChannelMock;
	private IDiscordChannel? _tournamentSignUpChannelMock;
	private MessageEventHandler? _handler;

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
		_rulesChannelMock.Id.Returns(_options.Value.ChannelIds.Rules);
		_generalChannelMock = Substitute.For<IDiscordChannel>();
		_generalChannelMock.Id.Returns(_options.Value.ChannelIds.General);
		_tournamentSignUpChannelMock = Substitute.For<IDiscordChannel>();
		_tournamentSignUpChannelMock.Id.Returns(_options.Value.ChannelIds.TournamentSignUp);

		_guildMock = Substitute.For<IDiscordGuild>();
		_guildMock.Id.Returns(_options!.Value.ServerId);
		_guildMock.GetChannel(_options.Value.ChannelIds.Rules).Returns(_rulesChannelMock);
		_guildMock.GetChannel(_options.Value.ChannelIds.General).Returns(_generalChannelMock);
		_guildMock.GetChannel(_options.Value.ChannelIds.TournamentSignUp).Returns(_tournamentSignUpChannelMock);

		_discordClientMock = Substitute.For<IDiscordClient>();
		_discordClientMock.GetGuildAsync(_options.Value.ServerId).Returns(Task.FromResult(_guildMock));

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

	[TestMethod]
	public void Register_RegistersAllHandlersAndEvents()
	{
		// Act.
		_handler!.Register(_discordClientMock!, Substitute.For<IBotState>());

		// Assert.
		_discordClientMock!.Received(1).MessageCreated += Arg.Any<AsyncEventHandler<DiscordClient, MessageCreateEventArgs>>();
		_discordClientMock!.Received(1).MessageDeleted += Arg.Any<AsyncEventHandler<DiscordClient, MessageDeleteEventArgs>>();
		_discordClientMock!.Received(1).MessageReactionAdded += Arg.Any<AsyncEventHandler<DiscordClient, MessageReactionAddEventArgs>>();
		_discordClientMock!.Received(1).MessageReactionRemoved += Arg.Any<AsyncEventHandler<DiscordClient, MessageReactionRemoveEventArgs>>();
	}

	[TestMethod]
	public async Task HandleMessageReactionAdded_ShouldProcessRulesReadEmoji()
	{
		// Arrange.
		ulong reactingUserId = 555UL;

		IDiscordRole membersRole = Substitute.For<IDiscordRole>();
		membersRole.Id.Returns(_options!.Value.RoleIds.Members);
		_guildMock!.GetRole(_options!.Value.RoleIds.Members).Returns(membersRole);

		// Emoji
		IDiscordEmoji emoji = Substitute.For<IDiscordEmoji>();
		emoji.GetDiscordName().Returns(":ok:");
		emoji.Name.Returns("ok");
		_discordMessageUtilsMock!.GetDiscordEmoji("ok").Returns(emoji);

		// Message + reaction users
		IDiscordMessage message = Substitute.For<IDiscordMessage>();
		IDiscordUser reactingUser = Substitute.For<IDiscordUser>();
		reactingUser.Id.Returns(reactingUserId);
		reactingUser.IsBot.Returns(false);
		message.GetReactionsAsync(emoji).Returns(Task.FromResult<IReadOnlyList<IDiscordUser>>([reactingUser]));

		// Member with MustReadRules role and display name triggering nickname change
		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.Id.Returns(reactingUserId);
		member.DisplayName.Returns("[NLBE] PlayerName");
		member.Username.Returns("PlayerName");
		IDiscordRole mustReadRulesRole = Substitute.For<IDiscordRole>();
		mustReadRulesRole.Id.Returns(_options!.Value.RoleIds.MustReadRules);
		member.Roles.Returns([mustReadRulesRole]);
		_guildMock.GetMemberAsync(reactingUserId).Returns(Task.FromResult(member));

		// User who added the reaction (not a bot)
		IDiscordUser addingUser = Substitute.For<IDiscordUser>();
		addingUser.IsBot.Returns(false);
		addingUser.Mention.Returns("@PlayerName");

		_handler!.Register(_discordClientMock!, Substitute.For<IBotState>());

		// Act.
		await _handler!.HandleMessageReactionAdded(message, _guildMock, _rulesChannelMock!, addingUser, emoji);

		// Assert.
		await message.Received(1).DeleteReactionAsync(emoji, reactingUser);
		await member.Received(1).RevokeRoleAsync(mustReadRulesRole);
		await _userServiceMock!.Received(1).ChangeMemberNickname(member, Arg.Is<string>(s => s.Equals("[] PlayerName")));
		await member.Received(1).GrantRoleAsync(membersRole);
		await _generalChannelMock!.Received(1).SendMessageAsync("@PlayerName, welkom op de NLBE discord server. Good luck, have fun!");
	}

	[DataTestMethod]
	[DataRow(MissingDependency.TournamentSignUpChannel)]
	[DataRow(MissingDependency.RulesChannel)]
	[DataRow(MissingDependency.GeneralChannel)]
	[DataRow(MissingDependency.MembersRole)]
	public async Task HandleMessageReactionAdded_ReturnsEarly_WhenDependencyIsNull(MissingDependency missing)
	{
		// Arrange.
		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.Id.Returns(999u);

		IDiscordChannel tournamentSignUpChannel = Substitute.For<IDiscordChannel>();
		IDiscordChannel rulesChannel = Substitute.For<IDiscordChannel>();
		IDiscordChannel generalChannel = Substitute.For<IDiscordChannel>();

		// Simulate null for the selected dependency
		switch (missing)
		{
			case MissingDependency.TournamentSignUpChannel:
				_guildMock!.GetChannel(_options!.Value.ChannelIds.TournamentSignUp).Returns((IDiscordChannel?) null);
				break;

			case MissingDependency.RulesChannel:
				_guildMock!.GetChannel(_options!.Value.ChannelIds.TournamentSignUp).Returns(tournamentSignUpChannel);
				_guildMock!.GetChannel(_options!.Value.ChannelIds.Rules).Returns((IDiscordChannel?) null);
				break;

			case MissingDependency.GeneralChannel:
				_guildMock!.GetChannel(_options!.Value.ChannelIds.TournamentSignUp).Returns(tournamentSignUpChannel);
				_guildMock!.GetChannel(_options!.Value.ChannelIds.Rules).Returns(rulesChannel);
				_guildMock!.GetChannel(_options!.Value.ChannelIds.General).Returns(generalChannel);
				break;

			case MissingDependency.MembersRole:
				_guildMock!.GetChannel(_options!.Value.ChannelIds.Welcome).Returns(tournamentSignUpChannel);
				_guildMock!.GetChannel(_options!.Value.ChannelIds.Rules).Returns(rulesChannel);
				_guildMock!.GetChannel(_options!.Value.ChannelIds.General).Returns(generalChannel);
				_guildMock!.GetRole(_options!.Value.RoleIds.Members).Returns((IDiscordRole?) null);
				break;
			default:
				break;
		}

		IBotState botState = Substitute.For<IBotState>();
		botState.IgnoreEvents.Returns(false);
		_handler!.Register(_discordClientMock!, botState);

		IDiscordMessage message = Substitute.For<IDiscordMessage>();
		IDiscordUser user = Substitute.For<IDiscordUser>();
		user.IsBot.Returns(false);
		IDiscordEmoji emoji = Substitute.For<IDiscordEmoji>();

		// Act.
		await _handler!.HandleMessageReactionAdded(message, _guildMock!, _rulesChannelMock!, user, emoji);

		// Assert.
		_guildMock!.Received().GetChannel(Arg.Any<ulong>());
		await _tournamentServiceMock!.DidNotReceive().GenerateLogMessage(Arg.Any<IDiscordMessage>(), Arg.Any<IDiscordChannel>(), Arg.Any<ulong>(), Arg.Any<string>());
		await message!.DidNotReceive().GetReactionsAsync(Arg.Any<IDiscordEmoji>());
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() =>
				new MessageEventHandler(null!, _loggerMock!, _userServiceMock!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, _mapServiceMock!, _replayServiceMock!, _tournamentServiceMock!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
				new MessageEventHandler(_options!, null!, _userServiceMock!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, _mapServiceMock!, _replayServiceMock!, _tournamentServiceMock!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
				new MessageEventHandler(_options!, _loggerMock!, null!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, _mapServiceMock!, _replayServiceMock!, _tournamentServiceMock!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
				new MessageEventHandler(_options!, _loggerMock!, _userServiceMock!, null!, _weeklyEventServiceMock!, _mapServiceMock!, _replayServiceMock!, _tournamentServiceMock!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
				new MessageEventHandler(_options!, _loggerMock!, _userServiceMock!, _discordMessageUtilsMock!, null!, _mapServiceMock!, _replayServiceMock!, _tournamentServiceMock!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
				new MessageEventHandler(_options!, _loggerMock!, _userServiceMock!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, null!, _replayServiceMock!, _tournamentServiceMock!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
				new MessageEventHandler(_options!, _loggerMock!, _userServiceMock!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, _mapServiceMock!, null!, _tournamentServiceMock!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
				new MessageEventHandler(_options!, _loggerMock!, _userServiceMock!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, _mapServiceMock!, _replayServiceMock!, null!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
				new MessageEventHandler(_options!, _loggerMock!, _userServiceMock!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, _mapServiceMock!, _replayServiceMock!, _tournamentServiceMock!, null!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
				new MessageEventHandler(_options!, _loggerMock!, _userServiceMock!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, _mapServiceMock!, _replayServiceMock!, _tournamentServiceMock!, _hallOfFameServiceMock!, null!));
	}
}
