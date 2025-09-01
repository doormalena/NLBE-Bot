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
public class BotEventHandlerTests
{
	private IOptions<BotOptions>? _optionsMock;
	private ICommandEventHandler? _commandHandlerMock;
	private IGuildMemberEventHandler? _guildMemberHandlerMock;
	private IMessageEventHandler? _messageHandlerMock;
	private IJob<VerifyServerNicknamesJob>? _verifyServerNicknamesJobMock;
	private IJob<AnnounceWeeklyWinnerJob>? _announceWeeklyWinnerJobMock;
	private ILogger<BotEventHandler>? _loggerMock;
	private IBotState? _botStateMock;
	private IDiscordClient? _discordClientMock;
	private ICommandsNextExtension? _commandsNextMock;
	private IDiscordGuild? _guildMock;
	private BotEventHandler? _handler;

	[TestInitialize]
	public void Setup()
	{
		_optionsMock = Substitute.For<IOptions<BotOptions>>();
		_optionsMock.Value.Returns(new BotOptions()
		{
			ChannelIds = new()
			{
				BotTest = 1234
			},
			ServerId = 1000000
		});

		_commandHandlerMock = Substitute.For<ICommandEventHandler>();
		_guildMemberHandlerMock = Substitute.For<IGuildMemberEventHandler>();
		_messageHandlerMock = Substitute.For<IMessageEventHandler>();
		_verifyServerNicknamesJobMock = Substitute.For<IJob<VerifyServerNicknamesJob>>();
		_announceWeeklyWinnerJobMock = Substitute.For<IJob<AnnounceWeeklyWinnerJob>>();
		_loggerMock = Substitute.For<ILogger<BotEventHandler>>();
		_botStateMock = Substitute.For<IBotState>();
		_discordClientMock = Substitute.For<IDiscordClient>();
		_commandsNextMock = Substitute.For<ICommandsNextExtension>();
		_guildMock = Substitute.For<IDiscordGuild>();

		_discordClientMock.GetGuildAsync(_optionsMock.Value.ServerId).Returns(_guildMock);
		_discordClientMock.GetCommandsNext().Returns(_commandsNextMock);

		_handler = new(_commandHandlerMock, _guildMemberHandlerMock, _messageHandlerMock, _verifyServerNicknamesJobMock, _announceWeeklyWinnerJobMock, _loggerMock, _optionsMock);
	}

	[TestMethod]
	public void Register_RegistersAllHandlersAndEvents()
	{
		// Act.
		_handler!.Register(_discordClientMock!, _botStateMock!);

		// Assert.
		_commandHandlerMock!.Received(1).Register(_commandsNextMock!);
		_guildMemberHandlerMock!.Received(1).Register(_discordClientMock!, _botStateMock!);
		_messageHandlerMock!.Received(1).Register(_discordClientMock!, _botStateMock!);
	}

	[TestMethod]
	public void HandleClienErrored_CallsErrorHandler_WithCorrectArguments()
	{
		// Arrange.
		string eventName = "TestEvent";
		Exception exception = new("Test exception");

		// Act.
		_handler!.HandleClienErrored(eventName, exception);

		// Assert.
		_loggerMock!.Received().Log(
					LogLevel.Error,
					Arg.Any<EventId>(),
					Arg.Is<object>(v => v.ToString()!.Contains(eventName)),
					exception,
					Arg.Any<Func<object, Exception?, string>>());
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

		_discordClientMock!.Guilds.Returns(guilds);

		// Act.
		await _handler!.HandleReady(_discordClientMock);

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
		Exception ex = new("Test exception");
		faultyClient.Guilds.Returns(x => { throw ex; });

		// Act.
		await _handler!.HandleReady(faultyClient);

		// Assert.
		_loggerMock!.Received().Log(
					LogLevel.Error,
					Arg.Any<EventId>(),
					Arg.Is<object>(v => v.ToString()!.Contains("Could not leave non-whitelisted guilds.")),
					ex,
					Arg.Any<Func<object, Exception?, string>>());
	}

	[TestMethod]
	public async Task HandleHeartbeated_SkipsFirstHeartbeat()
	{
		// Arrange.
		int ping = 123;
		DateTimeOffset timestamp = DateTimeOffset.Now;
		DateTime now = DateTime.Now;

		// Act.
		await _handler!.HandleHeartbeated(_discordClientMock!, ping, timestamp, now);

		// Assert.
		await _verifyServerNicknamesJobMock!.DidNotReceive().Execute(_guildMock!, Arg.Any<DateTime>());
		await _announceWeeklyWinnerJobMock!.DidNotReceive().Execute(_guildMock!, Arg.Any<DateTime>());
	}

	[TestMethod]
	public async Task HandleHeartbeated_ExecutesJobs_AfterFirstHeartbeat()
	{
		// Arrange.		
		int ping = 123;
		DateTimeOffset timestamp = DateTimeOffset.Now;
		DateTime now = DateTime.Now;

		// Act.
		await _handler!.HandleHeartbeated(_discordClientMock!, ping, timestamp, now); // First call (should skip).
		await _handler!.HandleHeartbeated(_discordClientMock!, ping, timestamp, now); // Second call (should execute jobs).

		// Assert.
		await _verifyServerNicknamesJobMock!.Received(1).Execute(_guildMock!, now);
		await _announceWeeklyWinnerJobMock!.Received(1).Execute(_guildMock!, now);
	}

	[TestMethod]
	public async Task HandleSocketClosed_AbnormalClosure()
	{
		// Act.
		await _handler!.HandleSocketClosed(4000, "Closed by test");

		// Assert.
		_loggerMock!.Received().Log(
					LogLevel.Error,
					Arg.Any<EventId>(),
					Arg.Is<object>(v => v.ToString()!.Contains("Socket closed unexpectedly.")),
					Arg.Any<Exception>(),
					Arg.Any<Func<object, Exception?, string>>());
	}

	[TestMethod]
	public async Task HandleSocketClosed_NormalClosure()
	{
		// Act.
		await _handler!.HandleSocketClosed(1000, "Closed by test");

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
		Assert.ThrowsException<ArgumentNullException>(() => _handler!.Register(null!, _botStateMock!));
		Assert.ThrowsException<ArgumentNullException>(() => _handler!.Register(_discordClientMock!, null!));
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new BotEventHandler(null!, _guildMemberHandlerMock!, _messageHandlerMock!, _verifyServerNicknamesJobMock!, _announceWeeklyWinnerJobMock!, _loggerMock!, _optionsMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandler(_commandHandlerMock!, null!, _messageHandlerMock!, _verifyServerNicknamesJobMock!, _announceWeeklyWinnerJobMock!, _loggerMock!, _optionsMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandler(_commandHandlerMock!, _guildMemberHandlerMock!, null!, _verifyServerNicknamesJobMock!, _announceWeeklyWinnerJobMock!, _loggerMock!, _optionsMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandler(_commandHandlerMock!, _guildMemberHandlerMock!, _messageHandlerMock!, null!, _announceWeeklyWinnerJobMock!, _loggerMock!, _optionsMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandler(_commandHandlerMock!, _guildMemberHandlerMock!, _messageHandlerMock!, _verifyServerNicknamesJobMock!, null!, _loggerMock!, _optionsMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandler(_commandHandlerMock!, _guildMemberHandlerMock!, _messageHandlerMock!, _verifyServerNicknamesJobMock!, _announceWeeklyWinnerJobMock!, null!, _optionsMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandler(_commandHandlerMock!, _guildMemberHandlerMock!, _messageHandlerMock!, _verifyServerNicknamesJobMock!, _announceWeeklyWinnerJobMock!, _loggerMock!, null!));
	}
}
