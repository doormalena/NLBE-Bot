namespace NLBE_Bot.Tests.EventHandlers;

using DSharpPlus;
using Microsoft.Extensions.Logging;
using NLBE_Bot.EventHandlers;
using NLBE_Bot.Interfaces;
using NSubstitute;
using System;

[TestClass]
public class BotEventHandlersTests
{
	private ICommandEventHandler? _commandHandler;
	private IGuildMemberEventHandler? _guildMemberHandler;
	private IMessageEventHandler? _messageHandler;
	private IUserService? _userService;
	private IWeeklyEventService? _weeklyEventService;
	private ILogger<BotEventHandlers>? _logger;
	private IErrorHandler? _errorHandler;
	private IBotState? _botState;
	private IDiscordClient? _client;
	private ICommandsNextExtension? _commandsNext;
	private BotEventHandlers? _handlers;

	[TestInitialize]
	public void Setup()
	{
		_commandHandler = Substitute.For<ICommandEventHandler>();
		_guildMemberHandler = Substitute.For<IGuildMemberEventHandler>();
		_messageHandler = Substitute.For<IMessageEventHandler>();
		_userService = Substitute.For<IUserService>();
		_weeklyEventService = Substitute.For<IWeeklyEventService>();
		_logger = Substitute.For<ILogger<BotEventHandlers>>();
		_errorHandler = Substitute.For<IErrorHandler>();
		_botState = Substitute.For<IBotState>();
		_client = Substitute.For<IDiscordClient>();
		_commandsNext = Substitute.For<ICommandsNextExtension>();

		_client.GetCommandsNext().Returns(_commandsNext);

		_handlers = new(_commandHandler, _guildMemberHandler, _messageHandler, _userService, _weeklyEventService, _logger, _errorHandler);
	}

	[TestMethod]
	public void Register_RegistersAllHandlersAndEvents()
	{
		// Act.
		_handlers!.Register(_client, _botState);

		// Assert.
		_commandHandler!.Received(1).Register(_commandsNext);
		_guildMemberHandler!.Received(1).Register(_client);
		_messageHandler!.Received(1).Register(_client);
	}

	[TestMethod]
	public async Task HandleReady_LeavesNonWhitelistedGuilds_AndLogs()
	{
		// Arrange.
		IDiscordGuild guild1 = Substitute.For<IDiscordGuild>();
		guild1.Id.Returns(1234UL);
		IDiscordGuild guild2 = Substitute.For<IDiscordGuild>();
		guild2.Id.Returns(Constants.NLBE_SERVER_ID);
		IDiscordGuild guild3 = Substitute.For<IDiscordGuild>();
		guild3.Id.Returns(Constants.DA_BOIS_ID);

		Dictionary<ulong, IDiscordGuild> guilds = new()
		{
			{ 1234UL, guild1 },
			{ Constants.NLBE_SERVER_ID, guild2 },
			{ Constants.DA_BOIS_ID, guild3 }
		};

		_client!.Guilds.Returns(guilds);

		// Act.
		await _handlers!.HandleReady(_client);

		// Assert.
		await guild1.Received(1).LeaveAsync();
		await guild2.DidNotReceive().LeaveAsync();
		await guild3.DidNotReceive().LeaveAsync();
		_logger!.Received().Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task HandleHeartbeated_SkipsFirstHeartbeat()
	{
		// Arrange.
		DateTime now = DateTime.Now;

		// Act.
		await _handlers!.HandleHeartbeated(now);

		// Assert.
		await _userService!.DidNotReceive().UpdateUsers();
		await _weeklyEventService!.DidNotReceive().AnnounceWeeklyWinner();
	}

	[TestMethod]
	public async Task HandleHeartbeated_DoesNothing_WhenIgnoreEvents()
	{
		// Arrange.
		DateTime now = DateTime.Now;
		_botState!.IgnoreEvents.Returns(true);
		_handlers!.Register(_client, _botState);

		// Act.
		await _handlers.HandleHeartbeated(now); // Skipped.
		await _handlers.HandleHeartbeated(now); // Simulate second heartbeat

		await _userService!.DidNotReceive().UpdateUsers();
		await _weeklyEventService!.DidNotReceive().AnnounceWeeklyWinner();
	}

	[TestMethod]
	public async Task UpdateUsernames_Updates_WhenNotUpdatedToday()
	{
		// Arrange.
		DateTime now = DateTime.Now;
		DateTime yesterday = DateTime.Now.AddDays(-1);
		_botState!.LasTimeNamesWereUpdated.Returns(yesterday);
		_handlers!.Register(_client, _botState);

		// Act.
		await _handlers.HandleHeartbeated(now); // Skipped.
		await _handlers.HandleHeartbeated(now); // Simulate second heartbeat (should trigger update).

		// Assert.
		await _userService!.Received(1).UpdateUsers();
		_botState.Received().LasTimeNamesWereUpdated = Arg.Is<DateTime>(dt => dt.Date == now.Date);
	}

	[TestMethod]
	public async Task HandleHeartbeated_DoesNotUpdateUsernames_WhenAlreadyUpdatedToday()
	{
		// Arrange.
		DateTime now = DateTime.Now;
		_botState!.LasTimeNamesWereUpdated.Returns(now);
		_handlers!.Register(_client, _botState);

		// Act.
		await _handlers.HandleHeartbeated(now); // Skipped.
		await _handlers.HandleHeartbeated(now); // Simulate second heartbeat (should NOT trigger update).

		// Assert
		await _userService!.DidNotReceive().UpdateUsers();
		_botState.DidNotReceive().LasTimeNamesWereUpdated = Arg.Any<DateTime>();
	}

	[TestMethod]
	public async Task HandleHeartbeated_AnnouncesWeeklyWinner_WhenMondayAfter14_AndNotAnnouncedThisWeek()
	{
		// Arrange: Monday, 14:00, last announcement was a week ago
		DateTime monday14 = new(2025, 6, 23, 14, 0, 0, DateTimeKind.Local); // Monday 14:00
		DateTime lastAnnouncement = monday14.AddDays(-7);
		_botState!.LastWeeklyWinnerAnnouncement.Returns(lastAnnouncement);
		_handlers!.Register(_client, _botState);

		// Act.
		await _handlers.HandleHeartbeated(monday14); // First heartbeat (skipped).													 
		await _handlers.HandleHeartbeated(monday14); // Second heartbeat (should trigger weekly winner).

		// Assert.
		await _weeklyEventService!.Received(1).AnnounceWeeklyWinner();
		_botState.Received().LastWeeklyWinnerAnnouncement = monday14;
	}

	[TestMethod]
	public async Task HandleHeartbeated_DoesNotAnnounceWeeklyWinner_WhenNotMondayAfter14()
	{
		// Arrange: Tuesday, 14:00, last announcement was yesterday
		DateTime tuesday14 = new(2025, 6, 24, 14, 0, 0, DateTimeKind.Local); // Tuesday 14:00
		DateTime lastAnnouncement = tuesday14.AddDays(-1);
		_botState!.LastWeeklyWinnerAnnouncement.Returns(lastAnnouncement);
		_handlers!.Register(_client, _botState);

		// Act.
		await _handlers.HandleHeartbeated(tuesday14); // First heartbeat (skipped).
		await _handlers.HandleHeartbeated(tuesday14); // Second heartbeat (should NOT trigger weekly winner).

		// Assert.
		await _weeklyEventService!.DidNotReceive().AnnounceWeeklyWinner();
		_botState.DidNotReceive().LastWeeklyWinnerAnnouncement = Arg.Any<DateTime>();
	}

	[TestMethod]
	public async Task UpdateUsernames_HandlesException()
	{
		// Arrange.
		DateTime yesterday = DateTime.Now.AddDays(-1);
		DateTime now = DateTime.Now;
		_botState!.LasTimeNamesWereUpdated.Returns(yesterday);
		_userService!.UpdateUsers().Returns(x => { throw new Exception("fail"); });
		_handlers!.Register(_client, _botState);

		// Act.
		await _handlers.HandleHeartbeated(now); // Skipped.
		await _handlers.HandleHeartbeated(now); // Simulate second heartbeat (should trigger update).

		// Assert.
		await _errorHandler!.Received().HandleErrorAsync(Arg.Is<string>(s => s.Contains("ERROR updating users")), Arg.Any<Exception>());
	}

	[TestMethod]
	public void Register_ThrowsArgumentNullException_WhenAnyParameterIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() => _handlers!.Register(null, _botState));
		Assert.ThrowsException<ArgumentNullException>(() => _handlers!.Register(_client, null));
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new BotEventHandlers(null, _guildMemberHandler, _messageHandler, _userService, _weeklyEventService, _logger, _errorHandler));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandler, null, _messageHandler, _userService, _weeklyEventService, _logger, _errorHandler));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandler, _guildMemberHandler, null, _userService, _weeklyEventService, _logger, _errorHandler));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandler, _guildMemberHandler, _messageHandler, null, _weeklyEventService, _logger, _errorHandler));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandler, _guildMemberHandler, _messageHandler, _userService, null, _logger, _errorHandler));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandler, _guildMemberHandler, _messageHandler, _userService, _weeklyEventService, null, _errorHandler));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandler, _guildMemberHandler, _messageHandler, _userService, _weeklyEventService, _logger, null));
	}
}
