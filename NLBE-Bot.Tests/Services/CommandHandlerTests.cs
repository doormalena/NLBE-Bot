namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using System;
using System.Threading.Tasks;

[TestClass]
public class CommandHandlerTests
{
	private Mock<ILogger<CommandHandler>>? _loggerMock;
	private Mock<IErrorHandler>? _errorHandlerMock;
	private Mock<IDiscordMessageUtils>? _discordMessageUtilsMock;
	private CommandHandler? _handler;

	[TestInitialize]
	public void Setup()
	{
		_loggerMock = new Mock<ILogger<CommandHandler>>();
		_errorHandlerMock = new Mock<IErrorHandler>();
		_discordMessageUtilsMock = new Mock<IDiscordMessageUtils>();
		_handler = new CommandHandler(_loggerMock.Object, _errorHandlerMock.Object, _discordMessageUtilsMock.Object);
	}

	[TestMethod]
	public async Task OnCommandExecuted_LogsCommandName()
	{
		// Arrange.
		Mock<ICommand> commandInfoMock = new();
		commandInfoMock.SetupGet(c => c.Name).Returns("testcmd");

		// Act.
		await _handler!.HandleCommandExecuted(commandInfoMock.Object);

		// Assert.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		_loggerMock.Verify(l => l.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("Command executed: testcmd")),
				null,
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
#pragma warning restore CS8602
	}

	[TestMethod]
	public async Task OnCommandErrored_IgnoresIfNotInAllowedGuilds()
	{
		// Arrange.
		Mock<ICommandContext> contextMock = new();
		contextMock.SetupGet(c => c.GuildId).Returns(123UL); // Not a valid guild ID

		// Act.
		await _handler!.HandleCommandError(contextMock.Object, null, null);

		// Assert.
		_errorHandlerMock!.Verify(e => e.HandleErrorAsync(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
	}

	[TestMethod]
	public async Task OnCommandErrored_SendsUnauthorizedMessage()
	{
		// Arrange
		Mock<ICommandContext> contextMock = new();
		contextMock.SetupGet(c => c.GuildId).Returns(Constants.NLBE_SERVER_ID);
		contextMock.Setup(c => c.SendUnauthorizedMessageAsync()).Returns(Task.CompletedTask);

		// Act
		await _handler!.HandleCommandError(contextMock.Object, null, new Exception("Unauthorized access"));

		// Assert
		contextMock.Verify(c => c.SendUnauthorizedMessageAsync(), Times.Once);
	}

	[TestMethod]
	public async Task OnCommandErrored_HandlesCommandError()
	{
		// Arrange
		Mock<ICommandContext> contextMock = new();
		contextMock.SetupGet(c => c.GuildId).Returns(Constants.NLBE_SERVER_ID);

		Mock<IDiscordEmoji> emojiInProgressMock = new();
		IDiscordEmoji emojiInProgress = emojiInProgressMock.Object;
		Mock<IDiscordEmoji> emojiErrorMock = new();
		IDiscordEmoji emojiError = emojiErrorMock.Object;

		_discordMessageUtilsMock!.Setup(d => d.GetDiscordEmoji(Constants.IN_PROGRESS_REACTION)).Returns(emojiInProgress);
		_discordMessageUtilsMock.Setup(d => d.GetDiscordEmoji(Constants.ERROR_REACTION)).Returns(emojiError);

		contextMock.Setup(c => c.DeleteInProgressReactionAsync(emojiInProgress)).Returns(Task.CompletedTask);
		contextMock.Setup(c => c.AddErrorReactionAsync(emojiError)).Returns(Task.CompletedTask);

		Mock<ICommand> commandMock = new();
		commandMock.SetupGet(c => c.Name).Returns("testcmd");

		_errorHandlerMock!.Setup(e => e.HandleErrorAsync(It.IsAny<string>(), It.IsAny<Exception>())).Returns(Task.CompletedTask);

		// Act
		await _handler!.HandleCommandError(contextMock.Object, commandMock.Object, new Exception("Some error"));

		// Assert
		contextMock.Verify(c => c.DeleteInProgressReactionAsync(emojiInProgress), Times.Once);
		contextMock.Verify(c => c.AddErrorReactionAsync(emojiError), Times.Once);
		_errorHandlerMock.Verify(e => e.HandleErrorAsync(It.Is<string>(s => s.Contains("testcmd")), It.IsAny<Exception>()), Times.Once);
	}
}

