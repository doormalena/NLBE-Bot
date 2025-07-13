namespace NLBE_Bot.Tests.EventHandlers;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.EventHandlers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Jobs;
using NSubstitute;
using System;

[TestClass]
public class BotEventHandlersTests
{
	private IOptions<BotOptions>? _optionsMock;
	private ICommandEventHandler? _commandHandlerMock;
	private IGuildMemberEventHandler? _guildMemberHandlerMock;
	private IMessageEventHandler? _messageHandlerMock;
	private IJob<VerifyServerNicknamesJob>? _verifyServerNicknamesJobMock;
	private IJob<AnnounceWeeklyWinnerJob>? _announceWeeklyWinnerJobMock;
	private ILogger<BotEventHandlers>? _loggerMock;
	private IErrorHandler? _errorHandlerMock;
	private IBotState? _botStateMock;
	private IDiscordClient? _clientMock;
	private ICommandsNextExtension? _commandsNextMock;
	private BotEventHandlers? _handlers;

	[TestInitialize]
	public void Setup()
	{
		_optionsMock = Substitute.For<IOptions<BotOptions>>();
		_optionsMock.Value.Returns(new BotOptions()
		{
			ServerId = 1000000
		});

		_commandHandlerMock = Substitute.For<ICommandEventHandler>();
		_guildMemberHandlerMock = Substitute.For<IGuildMemberEventHandler>();
		_messageHandlerMock = Substitute.For<IMessageEventHandler>();
		_verifyServerNicknamesJobMock = Substitute.For<IJob<VerifyServerNicknamesJob>>();
		_announceWeeklyWinnerJobMock = Substitute.For<IJob<AnnounceWeeklyWinnerJob>>();
		_loggerMock = Substitute.For<ILogger<BotEventHandlers>>();
		_errorHandlerMock = Substitute.For<IErrorHandler>();
		_botStateMock = Substitute.For<IBotState>();
		_clientMock = Substitute.For<IDiscordClient>();
		_commandsNextMock = Substitute.For<ICommandsNextExtension>();

		_clientMock.GetCommandsNext().Returns(_commandsNextMock);

		_handlers = new(_commandHandlerMock, _guildMemberHandlerMock, _messageHandlerMock, _verifyServerNicknamesJobMock, _announceWeeklyWinnerJobMock, _loggerMock, _errorHandlerMock, _optionsMock);
	}

	[TestMethod]
	public void Register_RegistersAllHandlersAndEvents()
	{
		// Act.
		_handlers!.Register(_clientMock, _botStateMock);

		// Assert.
		_commandHandlerMock!.Received(1).Register(_commandsNextMock);
		_guildMemberHandlerMock!.Received(1).Register(_clientMock, _botStateMock);
		_messageHandlerMock!.Received(1).Register(_clientMock);
	}

	[TestMethod]
	public async Task HandleClienErrored_CallsErrorHandler_WithCorrectArguments()
	{
		// Arrange.
		string eventName = "TestEvent";
		Exception exception = new("Test exception");

		// Act.
		await _handlers!.HandleClienErrored(eventName, exception);

		// Assert.
		await _errorHandlerMock!.Received(1).HandleErrorAsync(
			Arg.Is<string>(msg => msg.Contains(eventName)),
			exception
		);
	}

	[TestMethod]
	public async Task HandleReady_LeavesNonWhitelistedGuilds_AndLogs()
	{
		// Arrange.
		IDiscordGuild guild1 = Substitute.For<IDiscordGuild>();
		guild1.Id.Returns(1234UL);
		IDiscordGuild guild2 = Substitute.For<IDiscordGuild>();
		guild2.Id.Returns(1000000UL);

		Dictionary<ulong, IDiscordGuild> guilds = new()
		{
			{ 1234UL, guild1 },
			{ 1000000UL, guild2 },
		};

		_clientMock!.Guilds.Returns(guilds);

		// Act.
		await _handlers!.HandleReady(_clientMock);

		// Assert.
		await guild1.Received(1).LeaveAsync();
		await guild2.DidNotReceive().LeaveAsync();
		_loggerMock!.Received(1).Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task HandleReady_LogsError_WhenExceptionThrown()
	{
		// Arrange.
		IDiscordClient faultyClient = Substitute.For<IDiscordClient>();
		faultyClient.Guilds.Returns(x => { throw new Exception("Test exception"); });

		// Act.
		await _handlers!.HandleReady(faultyClient);

		// Assert.
		await _errorHandlerMock!.Received(1).HandleErrorAsync(
			Arg.Is<string>(msg => msg.Contains("HandleReady")),
			Arg.Any<Exception>()
		);
	}

	[TestMethod]
	public async Task HandleHeartbeated_SkipsFirstHeartbeat()
	{
		// Arrange.
		int ping = 123;
		DateTimeOffset timestamp = DateTimeOffset.Now;
		DateTime now = DateTime.Now;

		// Act.
		await _handlers!.HandleHeartbeated(ping, timestamp, now);

		// Assert.
		await _verifyServerNicknamesJobMock!.DidNotReceive().Execute(Arg.Any<DateTime>());
		await _announceWeeklyWinnerJobMock!.DidNotReceive().Execute(Arg.Any<DateTime>());
	}

	[TestMethod]
	public async Task HandleHeartbeated_ExecutesJobs_AfterFirstHeartbeat()
	{
		// Arrange.		
		int ping = 123;
		DateTimeOffset timestamp = DateTimeOffset.Now;
		DateTime now = DateTime.Now;

		// Act.
		await _handlers!.HandleHeartbeated(ping, timestamp, now); // First call (should skip).
		await _handlers!.HandleHeartbeated(ping, timestamp, now); // Second call (should execute jobs).

		// Assert.
		await _verifyServerNicknamesJobMock!.Received(1).Execute(now);
		await _announceWeeklyWinnerJobMock!.Received(1).Execute(now);
	}

	[TestMethod]
	public async Task HandleSocketClosed_AbnormalClosure()
	{
		// Act.
		await _handlers!.HandleSocketClosed(4000, "Closed by test");

		// Assert.
		await _errorHandlerMock!.Received(1).HandleErrorAsync(
			Arg.Is<string>(msg => msg.Contains("Socket closed unexpectedly.")),
			null
		);
	}

	[TestMethod]
	public async Task HandleSocketClosed_NormalClosure()
	{
		// Act.
		await _handlers!.HandleSocketClosed(1000, "Closed by test");

		// Assert.
		_loggerMock!.Received(1).Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v.ToString()!.Contains("Socket closed normally.")),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public void Register_ThrowsArgumentNullException_WhenAnyParameterIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() => _handlers!.Register(null, _botStateMock));
		Assert.ThrowsException<ArgumentNullException>(() => _handlers!.Register(_clientMock, null));
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new BotEventHandlers(null, _guildMemberHandlerMock, _messageHandlerMock, _verifyServerNicknamesJobMock, _announceWeeklyWinnerJobMock, _loggerMock, _errorHandlerMock, _optionsMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandlerMock, null, _messageHandlerMock, _verifyServerNicknamesJobMock, _announceWeeklyWinnerJobMock, _loggerMock, _errorHandlerMock, _optionsMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandlerMock, _guildMemberHandlerMock, null, _verifyServerNicknamesJobMock, _announceWeeklyWinnerJobMock, _loggerMock, _errorHandlerMock, _optionsMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandlerMock, _guildMemberHandlerMock, _messageHandlerMock, null, _announceWeeklyWinnerJobMock, _loggerMock, _errorHandlerMock, _optionsMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandlerMock, _guildMemberHandlerMock, _messageHandlerMock, _verifyServerNicknamesJobMock, null, _loggerMock, _errorHandlerMock, _optionsMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandlerMock, _guildMemberHandlerMock, _messageHandlerMock, _verifyServerNicknamesJobMock, _announceWeeklyWinnerJobMock, null, _errorHandlerMock, _optionsMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandlerMock, _guildMemberHandlerMock, _messageHandlerMock, _verifyServerNicknamesJobMock, _announceWeeklyWinnerJobMock, _loggerMock, null, _optionsMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandlerMock, _guildMemberHandlerMock, _messageHandlerMock, _verifyServerNicknamesJobMock, _announceWeeklyWinnerJobMock, _loggerMock, _errorHandlerMock, null));
	}
}
