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
	private IDiscordClient? _discordClientMock;
	private IBotEventHandlers? _eventHandlersMock;
	private ILogger<Bot>? _loggerMock;
	private IPublicIpAddress? _publicIpMock;
	private IServiceProvider? _serviceProviderMock;

	[TestInitialize]
	public void Setup()
	{
		_discordClientMock = Substitute.For<IDiscordClient>();
		_eventHandlersMock = Substitute.For<IBotEventHandlers>();
		_loggerMock = Substitute.For<ILogger<Bot>>();
		_publicIpMock = Substitute.For<IPublicIpAddress>();
		_serviceProviderMock = Substitute.For<IServiceProvider>();

		_publicIpMock!.GetPublicIpAddressAsync().Returns("1.2.3.4");

		ICommandsNextExtension commandsNextMock = Substitute.For<ICommandsNextExtension>();
		_discordClientMock.UseCommandsNext(Arg.Any<CommandsNextConfiguration>()).Returns(commandsNextMock);
		_discordClientMock.GetCommandsNext().Returns(commandsNextMock);
	}

	[TestMethod]
	public async Task ExecuteAsync_LogsStartupAndShutdown()
	{
		// Arrange.
		_discordClientMock!.ConnectAsync(Arg.Any<DiscordActivity>(), Arg.Any<UserStatus>())
								.Returns(Task.CompletedTask);

		Bot bot = new(
			_discordClientMock,
			_eventHandlersMock,
			_loggerMock,
			_publicIpMock,
			_serviceProviderMock);

		using CancellationTokenSource cts = new();
		cts.CancelAfter(10); // Cancel quickly to complete task.

		// Act.
		await bot.StartAsync(cts.Token);
		await Task.Delay(200); // Workaround to give the logger time to flush, otherwise causing the test to fail.

		// Assert.
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
		// Arrange.
		_discordClientMock!.ConnectAsync(Arg.Any<DiscordActivity>(), Arg.Any<UserStatus>())
						.Returns(x => throw new OperationCanceledException());

		Bot bot = new(
			_discordClientMock,
			_eventHandlersMock,
			_loggerMock,
			_publicIpMock,
			_serviceProviderMock);

		// Act.
		await bot.StartAsync(CancellationToken.None);

		// Assert.
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
		// Assert.
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(null, _eventHandlersMock, _loggerMock, _publicIpMock, _serviceProviderMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(_discordClientMock, null, _loggerMock, _publicIpMock, _serviceProviderMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(_discordClientMock, _eventHandlersMock, null, _publicIpMock, _serviceProviderMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(_discordClientMock, _eventHandlersMock, _loggerMock, null, _serviceProviderMock));
		Assert.ThrowsException<ArgumentNullException>(() => new Bot(_discordClientMock, _eventHandlersMock, _loggerMock, _publicIpMock, null));
	}
}
