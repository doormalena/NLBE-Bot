namespace NLBE_Bot.Tests.EventHandlers;

using DSharpPlus.AsyncEvents;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLBE_Bot.Configuration;
using NLBE_Bot.EventHandlers;
using NLBE_Bot.Interfaces;
using NSubstitute;
using System;
using System.Threading.Tasks;

[TestClass]
public class CommandEventHandlerTests
{
	private IOptions<BotOptions>? _optionsMock;
	private ILogger<CommandEventHandler>? _loggerMock;
	private IDiscordMessageUtils? _discordMessageUtilsMock;
	private CommandEventHandler? _handler;

	[TestInitialize]
	public void Setup()
	{
		_optionsMock = Substitute.For<IOptions<BotOptions>>();
		_optionsMock.Value.Returns(new BotOptions()
		{
			ServerId = 1000000
		});
		_loggerMock = Substitute.For<ILogger<CommandEventHandler>>();
		_discordMessageUtilsMock = Substitute.For<IDiscordMessageUtils>();
		_handler = new CommandEventHandler(_loggerMock, _discordMessageUtilsMock, _optionsMock);
	}

	[TestMethod]
	public void Register_RegistersAllHandlersAndEvents()
	{
		// Arrange.
		ICommandsNextExtension commandsNextExtension = Substitute.For<ICommandsNextExtension>();

		// Act.
		_handler!.Register(commandsNextExtension);

		// Assert.
		commandsNextExtension.Received(1).CommandExecuted += Arg.Any<AsyncEventHandler<CommandsNextExtension, CommandExecutionEventArgs>>();
		commandsNextExtension.Received(1).CommandErrored += Arg.Any<AsyncEventHandler<CommandsNextExtension, CommandErrorEventArgs>>();
	}

	[TestMethod]
	public async Task HandleCommandExecuted_LogsCommandName()
	{
		// Arrange.
		IDiscordCommand commandInfoMock = Substitute.For<IDiscordCommand>();
		commandInfoMock.Name.Returns("testcmd");

		// Act.
		await _handler!.HandleCommandExecuted(commandInfoMock);

		// Assert.
		_loggerMock!.Received().Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("testcmd")),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task HandleCommandErrored_IgnoresIfNotInAllowedGuilds()
	{
		// Arrange.
		IDiscordCommandContext contextMock = Substitute.For<IDiscordCommandContext>();
		contextMock.GuildId.Returns(123UL); // Not a valid guild ID

		// Act.
		await _handler!.HandleCommandError(contextMock, null, null);

		// Assert.
		_loggerMock!.DidNotReceive().Log(
			Arg.Any<LogLevel>(),
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task HandleCommandError_SendsUnauthorizedMessage()
	{
		// Arrange.
		IDiscordCommandContext contextMock = Substitute.For<IDiscordCommandContext>();
		contextMock.GuildId.Returns(1000000UL);
		contextMock.SendUnauthorizedMessageAsync().Returns(Task.CompletedTask);

		// Act.
		await _handler!.HandleCommandError(contextMock, null, new Exception("Unauthorized access"));

		// Assert.
		await contextMock.Received(1).SendUnauthorizedMessageAsync();
	}

	[TestMethod]
	public async Task HandleCommandError_HandlesCommandError()
	{
		// Arrange.
		IDiscordCommandContext contextMock = Substitute.For<IDiscordCommandContext>();
		contextMock.GuildId.Returns(1000000UL);

		IDiscordEmoji emojiInProgress = Substitute.For<IDiscordEmoji>();
		IDiscordEmoji emojiError = Substitute.For<IDiscordEmoji>();

		_discordMessageUtilsMock!.GetDiscordEmoji(Constants.IN_PROGRESS_REACTION).Returns(emojiInProgress);
		_discordMessageUtilsMock.GetDiscordEmoji(Constants.ERROR_REACTION).Returns(emojiError);

		contextMock.DeleteInProgressReactionAsync(emojiInProgress).Returns(Task.CompletedTask);
		contextMock.AddErrorReactionAsync(emojiError).Returns(Task.CompletedTask);

		IDiscordCommand commandMock = Substitute.For<IDiscordCommand>();
		commandMock.Name.Returns("testcmd");
		Exception ex = new("Some error");

		// Act.
		await _handler!.HandleCommandError(contextMock, commandMock, ex);

		// Assert.
		await contextMock.Received(1).DeleteInProgressReactionAsync(emojiInProgress);
		await contextMock.Received(1).AddErrorReactionAsync(emojiError);
		_loggerMock!.Received().Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v.ToString()!.Contains("testcmd")),
			ex,
			Arg.Any<Func<object, Exception?, string>>());
	}
}

