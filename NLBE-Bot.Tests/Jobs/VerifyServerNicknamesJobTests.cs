namespace NLBE_Bot.Tests.Jobs;

using DSharpPlus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Jobs;
using NSubstitute;
using System;

[TestClass]
public class VerifyServerNicknamesJobTests
{
	private IUserService? _userServiceMock;
	private IChannelService? _channelServiceMock;
	private IMessageService? _messageServcieMock;
	private IErrorHandler? _errorHandlerMock;
	private IBotState? _botStateMock;
	private IOptions<BotOptions>? _optionsMock;
	private ILogger<VerifyServerNicknamesJob>? _loggerMock;
	private VerifyServerNicknamesJob? _handler;

	[TestInitialize]
	public void Setup()
	{
		_optionsMock = Substitute.For<IOptions<BotOptions>>();
		_optionsMock.Value.Returns(new BotOptions()
		{

		});

		_userServiceMock = Substitute.For<IUserService>();
		_channelServiceMock = Substitute.For<IChannelService>();
		_messageServcieMock = Substitute.For<IMessageService>();
		_errorHandlerMock = Substitute.For<IErrorHandler>();
		_botStateMock = Substitute.For<IBotState>();
		_loggerMock = Substitute.For<ILogger<VerifyServerNicknamesJob>>();

		_handler = new(_userServiceMock, _channelServiceMock, _messageServcieMock, _errorHandlerMock, _optionsMock, _botStateMock, _loggerMock);
	}

	[TestMethod]
	public async Task Execute_VerifyServerNicknames_WhenNotVerifiedToday()
	{
		// Arrange.
		DateTime now = DateTime.Now;
		DateTime yesterday = DateTime.Now.AddDays(-1);
		_botStateMock!.LasTimeServerNicknamesWereVerified.Returns(yesterday);

		// Act.
		await _handler!.Execute(now);

		// Assert.
		await _channelServiceMock!.Received(1).GetBotTestChannel();
		_botStateMock.Received().LasTimeServerNicknamesWereVerified = Arg.Is<DateTime>(dt => dt.Date == now.Date);
	}

	[TestMethod]
	public async Task Execute_DoesNotVerifyServerNicknames_WhenAlreadyVerifiedToday()
	{
		// Arrange.
		DateTime now = DateTime.Now;
		_botStateMock!.LasTimeServerNicknamesWereVerified.Returns(now);

		// Act.
		await _handler!.Execute(now);

		// Assert
		await _channelServiceMock!.DidNotReceive().GetBotTestChannel();
		_botStateMock.DidNotReceive().LasTimeServerNicknamesWereVerified = Arg.Any<DateTime>();
	}

	[TestMethod]
	public async Task Execute_VerifyServerNicknames_HandlesException()
	{
		// Arrange.
		DateTime yesterday = DateTime.Now.AddDays(-1);
		DateTime now = DateTime.Now;
		_botStateMock!.LasTimeServerNicknamesWereVerified.Returns(yesterday);
		_channelServiceMock!.GetBotTestChannel().Returns<Task<IDiscordChannel>>(x => throw new Exception("fail"));

		// Act.
		await _handler!.Execute(now);

		// Assert.
		await _errorHandlerMock!.Received().HandleErrorAsync(Arg.Is<string>(s => s.Contains("An error occured while verifing all server nicknames.")), Arg.Any<Exception>());
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new VerifyServerNicknamesJob(null, _channelServiceMock, _messageServcieMock, _errorHandlerMock, _optionsMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, null, _messageServcieMock, _errorHandlerMock, _optionsMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, null, _errorHandlerMock, _optionsMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, _messageServcieMock, null, _optionsMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, _messageServcieMock, _errorHandlerMock, null, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, _messageServcieMock, _errorHandlerMock, _optionsMock, null, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, _messageServcieMock, _errorHandlerMock, _optionsMock, _botStateMock, null));
	}
}
