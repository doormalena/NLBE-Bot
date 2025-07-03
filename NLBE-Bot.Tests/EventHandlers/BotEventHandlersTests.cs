namespace NLBE_Bot.Tests.EventHandlers;

using DSharpPlus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLBE_Bot.EventHandlers;
using NLBE_Bot.Interfaces;
using NSubstitute;
using System;

[TestClass]
public class BotEventHandlersTests
{
	private IConfiguration? _configuration;
	private ICommandEventHandler? _commandHandler;
	private IGuildMemberEventHandler? _guildMemberHandler;
	private IMessageEventHandler? _messageHandler;
	private ITimedEventHandler? _timedEventHandler;
	private ILogger<BotEventHandlers>? _logger;
	private IErrorHandler? _errorHandler;
	private IBotState? _botState;
	private IDiscordClient? _client;
	private ICommandsNextExtension? _commandsNext;
	private BotEventHandlers? _handlers;

	[TestInitialize]
	public void Setup()
	{
		Dictionary<string, string> inMemorySettings = new()
		{
			{"NLBEBot:ServerId", "1000000"}
		};
		_configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(inMemorySettings!)
			.Build();
		_commandHandler = Substitute.For<ICommandEventHandler>();
		_guildMemberHandler = Substitute.For<IGuildMemberEventHandler>();
		_messageHandler = Substitute.For<IMessageEventHandler>();
		_timedEventHandler = Substitute.For<ITimedEventHandler>();
		_logger = Substitute.For<ILogger<BotEventHandlers>>();
		_errorHandler = Substitute.For<IErrorHandler>();
		_botState = Substitute.For<IBotState>();
		_client = Substitute.For<IDiscordClient>();
		_commandsNext = Substitute.For<ICommandsNextExtension>();

		_client.GetCommandsNext().Returns(_commandsNext);

		_handlers = new(_commandHandler, _guildMemberHandler, _messageHandler, _timedEventHandler, _logger, _errorHandler, _configuration);
	}

	[TestMethod]
	public void Register_RegistersAllHandlersAndEvents()
	{
		// Act.
		_handlers!.Register(_client, _botState);

		// Assert.
		_commandHandler!.Received(1).Register(_commandsNext);
		_guildMemberHandler!.Received(1).Register(_client, _botState);
		_messageHandler!.Received(1).Register(_client);
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

		_client!.Guilds.Returns(guilds);

		// Act.
		await _handlers!.HandleReady(_client);

		// Assert.
		await guild1.Received(1).LeaveAsync();
		await guild2.DidNotReceive().LeaveAsync();
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
		await _timedEventHandler!.DidNotReceive().Execute(Arg.Any<DateTime>());
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
			  new BotEventHandlers(null, _guildMemberHandler, _messageHandler, _timedEventHandler, _logger, _errorHandler, _configuration));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandler, null, _messageHandler, _timedEventHandler, _logger, _errorHandler, _configuration));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandler, _guildMemberHandler, null, _timedEventHandler, _logger, _errorHandler, _configuration));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandler, _guildMemberHandler, _messageHandler, null, _logger, _errorHandler, _configuration));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandler, _guildMemberHandler, _messageHandler, _timedEventHandler, null, _errorHandler, _configuration));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandler, _guildMemberHandler, _messageHandler, _timedEventHandler, _logger, null, _configuration));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new BotEventHandlers(_commandHandler, _guildMemberHandler, _messageHandler, _timedEventHandler, _logger, _errorHandler, null));
	}
}
