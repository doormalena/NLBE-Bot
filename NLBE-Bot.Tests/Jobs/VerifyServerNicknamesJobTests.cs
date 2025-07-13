namespace NLBE_Bot.Tests.Jobs;

using DSharpPlus;
using FMWOTB.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Jobs;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;

[TestClass]
public class VerifyServerNicknamesJobTests
{
	private IUserService? _userServiceMock;
	private IChannelService? _channelServiceMock;
	private IMessageService? _messageServcieMock;
	private IWGAccountService? _wgAcacountService;
	private IErrorHandler? _errorHandlerMock;
	private IBotState? _botStateMock;
	private IOptions<BotOptions>? _optionsMock;
	private ILogger<VerifyServerNicknamesJob>? _loggerMock;
	private VerifyServerNicknamesJob? _job;

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
		_wgAcacountService = Substitute.For<IWGAccountService>();
		_errorHandlerMock = Substitute.For<IErrorHandler>();
		_botStateMock = Substitute.For<IBotState>();
		_loggerMock = Substitute.For<ILogger<VerifyServerNicknamesJob>>();

		_job = new(_userServiceMock, _channelServiceMock, _messageServcieMock, _wgAcacountService, _errorHandlerMock, _optionsMock, _botStateMock, _loggerMock);
	}

	[TestMethod]
	public async Task Execute_VerifyServerNicknames_WhenNotVerifiedToday()
	{
		// Arrange.
		DateTime now = DateTime.Now;
		DateTime yesterday = DateTime.Now.AddDays(-1);
		_botStateMock!.LasTimeServerNicknamesWereVerified.Returns(yesterday);

		// Act.
		await _job!.Execute(now);

		// Assert.
		await _channelServiceMock!.Received(1).GetBotTestChannel();
		_botStateMock.Received().LasTimeServerNicknamesWereVerified = Arg.Is<DateTime>(dt => dt.Date == now.Date);
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
		await _job!.Execute(now);

		// Assert.
		await _errorHandlerMock!.Received().HandleErrorAsync(Arg.Is<string>(s => s.Contains("An error occured while verifing all server nicknames.")), Arg.Any<Exception>());
	}

	[TestMethod]
	public async Task Execute_DoesNothing_WhenAlreadyVerifiedToday()
	{
		// Arrange.
		_botStateMock!.LasTimeServerNicknamesWereVerified.Returns(DateTime.Today);

		// Act.
		await _job!.Execute(DateTime.Today);

		// Assert
		_botStateMock.DidNotReceive().LasTimeServerNicknamesWereVerified = Arg.Any<DateTime>();
	}

	[TestMethod]
	public async Task VerifyServerNicknames_LogsWarning_WhenChannelIsNull()
	{
		// Arrange.
		_channelServiceMock!.GetBotTestChannel().Returns((IDiscordChannel?) null);

		// Act.
		await _job!.Execute(DateTime.Today);

		// Assert.
		_loggerMock!.Received().Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("Could not find the bot test channel")),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task VerifyServerNicknames_SendsNoChangesMessage_WhenNoInvalidMatches()
	{
		// Arrange,
		IDiscordChannel channelMock = Substitute.For<IDiscordChannel>();
		IDiscordGuild guildMock = Substitute.For<IDiscordGuild>();
		channelMock.Guild.Returns(guildMock);
		_channelServiceMock!.GetBotTestChannel().Returns(channelMock);

		IDiscordRole memberRole = Substitute.For<IDiscordRole>();
		guildMock.GetRole(Arg.Any<ulong>()).Returns(memberRole);

		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.IsBot.Returns(false);
		member.Roles.Returns([memberRole]);
		member.DisplayName.Returns("[NLBE] Player");
		member.Username.Returns("Player");
		guildMock.GetAllMembersAsync().Returns([member]);

		_userServiceMock!.GetWotbPlayerNameFromDisplayName(Arg.Any<string>()).Returns(new Tuple<string, string>("[NLBE]", "Player"));

		IWGClan wgClan = Substitute.For<IWGClan>();
		wgClan.Tag.Returns("NLBE");
		IWGAccount wgAccount = Substitute.For<IWGAccount>();
		wgAccount.Nickname.Returns("Player");
		wgAccount.Clan.Returns(wgClan);
		_wgAcacountService!.SearchByName(Arg.Any<SearchAccuracy>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>())
			.Returns(Task.FromResult<IReadOnlyList<IWGAccount>>([wgAccount]));

		// Act.
		await _job!.Execute(DateTime.Today);

		// Assert.
		await channelMock.Received().SendMessageAsync(Arg.Is<string>(s => s.Contains("geen wijzigingen waren nodig")));
		_loggerMock!.Received().Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("no changes were necessary")),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task VerifyServerNicknames_CallsErrorHandler_OnException()
	{
		//Arrange.
		_channelServiceMock!.GetBotTestChannel().Throws(new Exception("Test exception"));

		// Arrange.
		await _job!.Execute(DateTime.Today);

		// Assert.
		await _errorHandlerMock!.Received().HandleErrorAsync(Arg.Any<string>(), Arg.Any<Exception>());
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new VerifyServerNicknamesJob(null, _channelServiceMock, _messageServcieMock, _wgAcacountService, _errorHandlerMock, _optionsMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, null, _messageServcieMock, _wgAcacountService, _errorHandlerMock, _optionsMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, null, _wgAcacountService, _errorHandlerMock, _optionsMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, _messageServcieMock, null, _errorHandlerMock, _optionsMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, _messageServcieMock, _wgAcacountService, null, _optionsMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, _messageServcieMock, _wgAcacountService, _errorHandlerMock, null, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, _messageServcieMock, _wgAcacountService, _errorHandlerMock, _optionsMock, null, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, _messageServcieMock, _wgAcacountService, _errorHandlerMock, _optionsMock, _botStateMock, null));
	}
}
