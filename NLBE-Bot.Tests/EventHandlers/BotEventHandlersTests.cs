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
		_loggerMock!.Received().Log(
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
		await _verifyServerNicknamesJobMock!.DidNotReceive().Execute(Arg.Any<DateTime>());
		await _announceWeeklyWinnerJobMock!.DidNotReceive().Execute(Arg.Any<DateTime>());
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
