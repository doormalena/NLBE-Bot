namespace NLBE_Bot.Tests.Services;

using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;

[TestClass]
public class BotTests
{
	private ILogger<Bot>? _loggerMock;
	private IDiscordClient? _discordClientMock;
	private ICommandEventHandler? _commandHandlerMock;
	private IWeeklyEventService? _weeklyEventHandlerMock;
	private IGuildMemberEventHandler? _guildMemberHandlerMock;
	private IBotState? _botStateMock;
	private IChannelService? _channelServiceMock;
	private IGuildProvider? _guildProviderMock;
	private IUserService? _userServiceMock;
	private IMessageService? _messageServiceMock;
	private IMessageEventHandler? _messageHandlerMock;
	private IServiceProvider? _serviceProviderMock;
	private IWeeklyEventService? _weeklyEventServiceMock;
	private IPublicIpAddress? _publicIpMock;

	[TestInitialize]
	public void Setup()
	{
		_loggerMock = Substitute.For<ILogger<Bot>>();
		_commandHandlerMock = Substitute.For<ICommandEventHandler>();
		_weeklyEventHandlerMock = Substitute.For<IWeeklyEventService>();
		_guildMemberHandlerMock = Substitute.For<IGuildMemberEventHandler>();
		_botStateMock = Substitute.For<IBotState>();
		_channelServiceMock = Substitute.For<IChannelService>();
		_guildProviderMock = Substitute.For<IGuildProvider>();
		_userServiceMock = Substitute.For<IUserService>();
		_messageServiceMock = Substitute.For<IMessageService>();
		_messageHandlerMock = Substitute.For<IMessageEventHandler>();
		_serviceProviderMock = Substitute.For<IServiceProvider>();
		_weeklyEventServiceMock = Substitute.For<IWeeklyEventService>();
		_publicIpMock = Substitute.For<IPublicIpAddress>();
		_discordClientMock = Substitute.For<IDiscordClient>();

		_publicIpMock!.GetPublicIpAddressAsync().Returns("1.2.3.4");

		ICommandsNextExtension commandsNextMock = Substitute.For<ICommandsNextExtension>();
		_discordClientMock.UseCommandsNext(Arg.Any<CommandsNextConfiguration>()).Returns(commandsNextMock);
		_discordClientMock.GetCommandsNext().Returns(commandsNextMock);
	}

	[TestMethod]
	public async Task ExecuteAsync_LogsStartupAndShutdown()
	{
		// Arrange
		_discordClientMock!.ConnectAsync(Arg.Any<DiscordActivity>(), Arg.Any<UserStatus>())
								.Returns(Task.CompletedTask);

		Bot bot = new(
			_discordClientMock,
			_serviceProviderMock,
			_commandHandlerMock,
			_weeklyEventHandlerMock,
			_guildMemberHandlerMock,
			_botStateMock,
			_channelServiceMock,
			_guildProviderMock,
			_userServiceMock,
			_messageServiceMock,
			_messageHandlerMock,
			_weeklyEventServiceMock,
			_loggerMock,
			_publicIpMock);

		using CancellationTokenSource cts = new();
		cts.CancelAfter(10); // Cancel quickly to complete task.

		// Act
		await bot.StartAsync(cts.Token);
		await Task.Delay(200); // Workaround to give the logger time to flush, otherwise causing the test to fail.

		// Assert
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		_loggerMock!.Received().Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v != null && v.ToString().Contains("NLBE Bot is starting.")),
			null,
			Arg.Any<Func<object, Exception?, string>>());

		_loggerMock!.Received().Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v != null &&
				(v.ToString().Contains("NLBE Bot is stopped.") ||
					v.ToString().Contains("NLBE Bot was cancelled gracefully."))),
			Arg.Any<Exception?>(),
			Arg.Any<Func<object, Exception?, string>>());
#pragma warning restore CS8602
	}

	[TestMethod]
	public async Task ExecuteAsync_HandlesOperationCanceledException()
	{
		// Arrange
		_discordClientMock!.ConnectAsync(Arg.Any<DiscordActivity>(), Arg.Any<UserStatus>())
						.Returns(x => throw new OperationCanceledException());

		Bot bot = new(
			_discordClientMock,
			_serviceProviderMock,
			_commandHandlerMock,
			_weeklyEventHandlerMock,
			_guildMemberHandlerMock,
			_botStateMock,
			_channelServiceMock,
			_guildProviderMock,
			_userServiceMock,
			_messageServiceMock,
			_messageHandlerMock,
			_weeklyEventServiceMock,
			_loggerMock,
			_publicIpMock);

		// Act
		await bot.StartAsync(CancellationToken.None);

		// Assert
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		_loggerMock.Received().Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v != null && v.ToString().Contains("NLBE Bot was cancelled gracefully.")),
			Arg.Any<OperationCanceledException>(),
			Arg.Any<Func<object, Exception?, string>>());
#pragma warning restore CS8602
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Test each dependency for null
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(
			null, _serviceProviderMock, _commandHandlerMock, _weeklyEventHandlerMock, _guildMemberHandlerMock, _botStateMock,
			_channelServiceMock, _guildProviderMock, _userServiceMock, _messageServiceMock, _messageHandlerMock, _weeklyEventServiceMock, _loggerMock, _publicIpMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(
			_discordClientMock, null, _commandHandlerMock, _weeklyEventHandlerMock, _guildMemberHandlerMock, _botStateMock,
			_channelServiceMock, _guildProviderMock, _userServiceMock, _messageServiceMock, _messageHandlerMock, _weeklyEventServiceMock, _loggerMock, _publicIpMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(
			_discordClientMock, _serviceProviderMock, null, _weeklyEventHandlerMock, _guildMemberHandlerMock, _botStateMock,
			_channelServiceMock, _guildProviderMock, _userServiceMock, _messageServiceMock, _messageHandlerMock, _weeklyEventServiceMock, _loggerMock, _publicIpMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(
			_discordClientMock, _serviceProviderMock, _commandHandlerMock, null, _guildMemberHandlerMock, _botStateMock,
			_channelServiceMock, _guildProviderMock, _userServiceMock, _messageServiceMock, _messageHandlerMock, _weeklyEventServiceMock, _loggerMock, _publicIpMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(
			_discordClientMock, _serviceProviderMock, _commandHandlerMock, _weeklyEventHandlerMock, null, _botStateMock,
			_channelServiceMock, _guildProviderMock, _userServiceMock, _messageServiceMock, _messageHandlerMock, _weeklyEventServiceMock, _loggerMock, _publicIpMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(
			_discordClientMock, _serviceProviderMock, _commandHandlerMock, _weeklyEventHandlerMock, _guildMemberHandlerMock, null,
			_channelServiceMock, _guildProviderMock, _userServiceMock, _messageServiceMock, _messageHandlerMock, _weeklyEventServiceMock, _loggerMock, _publicIpMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(
			_discordClientMock, _serviceProviderMock, _commandHandlerMock, _weeklyEventHandlerMock, _guildMemberHandlerMock, _botStateMock,
			null, _guildProviderMock, _userServiceMock, _messageServiceMock, _messageHandlerMock, _weeklyEventServiceMock, _loggerMock, _publicIpMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(
			_discordClientMock, _serviceProviderMock, _commandHandlerMock, _weeklyEventHandlerMock, _guildMemberHandlerMock, _botStateMock,
			_channelServiceMock, null, _userServiceMock, _messageServiceMock, _messageHandlerMock, _weeklyEventServiceMock, _loggerMock, _publicIpMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(
			_discordClientMock, _serviceProviderMock, _commandHandlerMock, _weeklyEventHandlerMock, _guildMemberHandlerMock, _botStateMock,
			_channelServiceMock, _guildProviderMock, null, _messageServiceMock, _messageHandlerMock, _weeklyEventServiceMock, _loggerMock, _publicIpMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(
			_discordClientMock, _serviceProviderMock, _commandHandlerMock, _weeklyEventHandlerMock, _guildMemberHandlerMock, _botStateMock,
			_channelServiceMock, _guildProviderMock, _userServiceMock, null, _messageHandlerMock, _weeklyEventServiceMock, _loggerMock, _publicIpMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(
			_discordClientMock, _serviceProviderMock, _commandHandlerMock, _weeklyEventHandlerMock, _guildMemberHandlerMock, _botStateMock,
			_channelServiceMock, _guildProviderMock, _userServiceMock, _messageServiceMock, null, _weeklyEventServiceMock, _loggerMock, _publicIpMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(
			_discordClientMock, _serviceProviderMock, _commandHandlerMock, _weeklyEventHandlerMock, _guildMemberHandlerMock, _botStateMock,
			_channelServiceMock, _guildProviderMock, _userServiceMock, _messageServiceMock, _messageHandlerMock, null, _loggerMock, _publicIpMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(
			_discordClientMock, _serviceProviderMock, _commandHandlerMock, _weeklyEventHandlerMock, _guildMemberHandlerMock, _botStateMock,
			_channelServiceMock, _guildProviderMock, _userServiceMock, _messageServiceMock, _messageHandlerMock, _weeklyEventServiceMock, null, _publicIpMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(
			_discordClientMock, _serviceProviderMock, _commandHandlerMock, _weeklyEventHandlerMock, _guildMemberHandlerMock, _botStateMock,
			_channelServiceMock, _guildProviderMock, _userServiceMock, _messageServiceMock, _messageHandlerMock, _weeklyEventServiceMock, _loggerMock, null));
	}
}
