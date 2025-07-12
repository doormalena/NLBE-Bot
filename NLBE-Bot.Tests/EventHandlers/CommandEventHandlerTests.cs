namespace NLBE_Bot.Tests.EventHandlers;

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
	private IErrorHandler? _errorHandlerMock;
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
		_errorHandlerMock = Substitute.For<IErrorHandler>();
		_discordMessageUtilsMock = Substitute.For<IDiscordMessageUtils>();
		_handler = new CommandEventHandler(_loggerMock, _errorHandlerMock, _discordMessageUtilsMock, _optionsMock);
	}

	[TestMethod]
	public async Task OnCommandExecuted_LogsCommandName()
	{
		// Arrange.
		IDiscordCommand commandInfoMock = Substitute.For<IDiscordCommand>();
		commandInfoMock.Name.Returns("testcmd");

		// Act.
		await _handler!.HandleCommandExecuted(commandInfoMock);

		// Assert.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		_loggerMock!.Received().Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v != null && v.ToString().Contains("Command executed: testcmd")),
			null,
			Arg.Any<Func<object, Exception?, string>>());
#pragma warning restore CS8602
	}

	[TestMethod]
	public async Task OnCommandErrored_IgnoresIfNotInAllowedGuilds()
	{
		// Arrange.
		IDiscordCommandContext contextMock = Substitute.For<IDiscordCommandContext>();
		contextMock.GuildId.Returns(123UL); // Not a valid guild ID

		// Act.
		await _handler!.HandleCommandError(contextMock, null, null);

		// Assert.
		await _errorHandlerMock!.DidNotReceiveWithAnyArgs().HandleErrorAsync(default!, default!);
	}

	[TestMethod]
	public async Task OnCommandErrored_SendsUnauthorizedMessage()
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
	public async Task OnCommandErrored_HandlesCommandError()
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

		_errorHandlerMock!.HandleErrorAsync(Arg.Any<string>(), Arg.Any<Exception>()).Returns(Task.CompletedTask);

		// Act.
		await _handler!.HandleCommandError(contextMock, commandMock, new Exception("Some error"));

		// Assert.
		await contextMock.Received(1).DeleteInProgressReactionAsync(emojiInProgress);
		await contextMock.Received(1).AddErrorReactionAsync(emojiError);
		await _errorHandlerMock.Received(1).HandleErrorAsync(
			Arg.Is<string>(s => s.Contains("testcmd")),
			Arg.Any<Exception>());
	}
}

