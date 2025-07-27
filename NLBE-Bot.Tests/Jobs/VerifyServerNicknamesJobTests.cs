namespace NLBE_Bot.Tests.Jobs;

using DSharpPlus;
using FMWOTB.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Jobs;
using NLBE_Bot.Models;
using NSubstitute;
using System;

[TestClass]
public class VerifyServerNicknamesJobTests
{
	private IUserService? _userServiceMock;
	private IChannelService? _channelServiceMock;
	private IMessageService? _messageServcieMock;
	private IWGAccountService? _wgAcacountServiceMock;
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
			WarGamingAppId = "dummy",
			MemberDefaultRoleId = 1234567890,
		});

		_userServiceMock = Substitute.For<IUserService>();
		_channelServiceMock = Substitute.For<IChannelService>();
		_messageServcieMock = Substitute.For<IMessageService>();
		_wgAcacountServiceMock = Substitute.For<IWGAccountService>();
		_errorHandlerMock = Substitute.For<IErrorHandler>();
		_botStateMock = Substitute.For<IBotState>();
		_loggerMock = Substitute.For<ILogger<VerifyServerNicknamesJob>>();

		_job = new(_userServiceMock, _channelServiceMock, _messageServcieMock, _wgAcacountServiceMock, _errorHandlerMock, _optionsMock, _botStateMock, _loggerMock);
	}

	[TestMethod]
	public async Task Execute_WhenNotVerifiedToday()
	{
		// Arrange.
		DateTime now = DateTime.Now;
		DateTime yesterday = DateTime.Now.AddDays(-1);
		_botStateMock!.LasTimeServerNicknamesWereVerified.Returns(yesterday);

		// Act.
		await _job!.Execute(now);

		// Assert.
		await _channelServiceMock!.Received(1).GetBotTestChannel();
		Assert.AreEqual(_botStateMock!.LasTimeServerNicknamesWereVerified, now);
	}

	[TestMethod]
	public async Task Execute_HandlesException()
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
		Assert.AreEqual(_botStateMock!.LasTimeServerNicknamesWereVerified, yesterday);
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
	public async Task Execute_LogsWarning_WhenChannelIsNull()
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
	public async Task Execute_LogsWarning_WhenDefaultMemberRoleIsNull()
	{
		// Arrange.
		IDiscordChannel testChannel = Substitute.For<IDiscordChannel>();
		IDiscordGuild testGuild = Substitute.For<IDiscordGuild>();
		testChannel.Guild.Returns(testGuild);
		_channelServiceMock!.GetBotTestChannel().Returns(Task.FromResult(testChannel));
		testGuild.GetRole(Arg.Any<ulong>()).Returns((IDiscordRole?) null);

		// Act.
		await _job!.Execute(DateTime.UtcNow);

		// Assert.
		_loggerMock!.Received().Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains($"Could not find the default member role with id `{_optionsMock!.Value.MemberDefaultRoleId}`. Aborting user update.")),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
		await testGuild.DidNotReceive().GetAllMembersAsync();
	}

	[TestMethod]
	public async Task Execute_SendsNoChangesMessage_WhenNoInvalidMatches()
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

		_userServiceMock!.GetWotbPlayerNameFromDisplayName(Arg.Any<string>()).Returns(new WotbPlayerNameInfo("[NLBE]", "Player"));

		IWGClan wgClan = Substitute.For<IWGClan>();
		wgClan.Tag.Returns("NLBE");
		IWGAccount wgAccount = Substitute.For<IWGAccount>();
		wgAccount.Nickname.Returns("Player");
		wgAccount.Clan.Returns(wgClan);
		_wgAcacountServiceMock!.SearchByName(Arg.Any<SearchAccuracy>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>())
			.Returns(Task.FromResult<IReadOnlyList<IWGAccount>>([wgAccount]));

		// Act.
		await _job!.Execute(DateTime.Today);

		// Assert.
		await channelMock.Received().SendMessageAsync(Arg.Is<string>(s => s.Contains("geen wijzigingen waren nodig")));
		_loggerMock!.Received().Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("All nicknames have been reviewed; no changes were necessary")),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task Execute_SendsMessages_ForMultipleInvalidPlayerClanMatches()
	{
		// Arrange.
		IDiscordChannel testChannel = Substitute.For<IDiscordChannel>();
		IDiscordGuild testGuild = Substitute.For<IDiscordGuild>();
		testChannel.Guild.Returns(testGuild);
		_channelServiceMock!.GetBotTestChannel().Returns(Task.FromResult(testChannel));

		IDiscordRole memberRole = Substitute.For<IDiscordRole>();
		testGuild.GetRole(Arg.Any<ulong>()).Returns(memberRole);

		IDiscordMember member1 = Substitute.For<IDiscordMember>(); // Simulate missing clan tag.
		member1.IsBot.Returns(false);
		member1.Roles.Returns([memberRole]);
		member1.DisplayName.Returns("Player1");
		member1.Username.Returns("Player1");

		IDiscordMember member2 = Substitute.For<IDiscordMember>(); // Simulate wrong clan tag.
		member2.IsBot.Returns(false);
		member2.Roles.Returns([memberRole]);
		member2.DisplayName.Returns("[NLBE2] Player2");
		member2.Username.Returns("Player2");

		testGuild.GetAllMembersAsync().Returns(Task.FromResult<IReadOnlyCollection<IDiscordMember>>(
			[member1, member2]
		));

		_userServiceMock!.GetWotbPlayerNameFromDisplayName(member1.DisplayName).Returns(new WotbPlayerNameInfo("", "Player1"));
		_userServiceMock!.GetWotbPlayerNameFromDisplayName(member2.DisplayName).Returns(new WotbPlayerNameInfo("[NLBE2]", "Player2"));

		IWGClan wgClan1 = Substitute.For<IWGClan>();
		wgClan1.Tag.Returns("NLBE");
		IWGAccount wgAccount1 = Substitute.For<IWGAccount>();
		wgAccount1.Nickname.Returns("Player1");
		wgAccount1.Clan.Returns(wgClan1);

		IWGClan wgClan2 = Substitute.For<IWGClan>();
		wgClan2.Tag.Returns("TAG");
		IWGAccount wgAccount2 = Substitute.For<IWGAccount>();
		wgAccount2.Nickname.Returns("Player2");
		wgAccount2.Clan.Returns(wgClan2);

		_wgAcacountServiceMock!.SearchByName(Arg.Any<SearchAccuracy>(), "Player1", Arg.Any<string>(), false, true, false)
			.Returns(Task.FromResult<IReadOnlyList<IWGAccount>>([wgAccount1]));
		_wgAcacountServiceMock!.SearchByName(Arg.Any<SearchAccuracy>(), "Player2", Arg.Any<string>(), false, true, false)
			.Returns(Task.FromResult<IReadOnlyList<IWGAccount>>([wgAccount2]));

		// Act.
		await _job!.Execute(DateTime.Today);

		// Assert.
		await _userServiceMock!.Received(2).ChangeMemberNickname(
			Arg.Any<IDiscordMember>(),
			Arg.Is<string>(s => s.Contains("Player1") || s.Contains("Player2"))
		);
		await _messageServcieMock!.Received(2).SendMessage(
			testChannel,
			null,
			testGuild.Name,
			Arg.Is<string>(msg => msg.Contains("is aangepast van"))
		);
		_loggerMock!.Received(2).Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("updated from")),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task Execute_SendsMessage_ForInvalidPlayerMatch()
	{
		// Arrange.
		IDiscordChannel testChannel = Substitute.For<IDiscordChannel>();
		IDiscordGuild testGuild = Substitute.For<IDiscordGuild>();
		testChannel.Guild.Returns(testGuild);
		_channelServiceMock!.GetBotTestChannel().Returns(Task.FromResult(testChannel));

		IDiscordRole memberRole = Substitute.For<IDiscordRole>();
		testGuild.GetRole(Arg.Any<ulong>()).Returns(memberRole);

		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.IsBot.Returns(false);
		member.Roles.Returns([memberRole]);
		member.DisplayName.Returns("[TAG] Player");
		member.Username.Returns("Player");

		testGuild.GetAllMembersAsync().Returns(Task.FromResult<IReadOnlyCollection<IDiscordMember>>(
			[member]
		));

		_userServiceMock!.GetWotbPlayerNameFromDisplayName(member.DisplayName).Returns(new WotbPlayerNameInfo("[TAG]", "Player"));
		_wgAcacountServiceMock!.SearchByName(Arg.Any<SearchAccuracy>(), "Player", Arg.Any<string>(), false, true, false)
			.Returns(Task.FromResult<IReadOnlyList<IWGAccount>>([]));

		// Act.
		await _job!.Execute(DateTime.Today);

		// Assert.
		await _messageServcieMock!.Received(1).SendPrivateMessage(
			member,
			testGuild.Name,
			Arg.Is<string>(s => s.Contains("Voor iedere gebruiker in de NLBE discord server wordt gecontroleerd of de ingestelde bijnaam overeenkomt met je WoTB spelersnaam.\nHelaas is dit voor jou niet het geval.\nWil je dit aanpassen?"))
		);
		await _messageServcieMock!.Received(1).SendMessage(
			testChannel,
			null,
			testGuild.Name,
			Arg.Is<string>(msg => msg.Contains("komt niet overeen met een WoTB-spelersnaam"))
		);
		_loggerMock!.Received().Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("does not match any WoTB player name")),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task Execute_Skips_Member_WhenIsBotOrNoRolesOrMissingRole()
	{
		// Arrange

		IDiscordChannel testChannel = Substitute.For<IDiscordChannel>();
		IDiscordGuild testGuild = Substitute.For<IDiscordGuild>();
		testChannel.Guild.Returns(testGuild);
		_channelServiceMock!.GetBotTestChannel().Returns(Task.FromResult(testChannel));

		IDiscordRole memberRole = Substitute.For<IDiscordRole>();
		testGuild.GetRole(Arg.Any<ulong>()).Returns(memberRole);

		// 1. Member is a bot
		IDiscordMember botMember = Substitute.For<IDiscordMember>();
		botMember.IsBot.Returns(true);
		botMember.Roles.Returns([memberRole]);

		// 2. Member has null roles
		IDiscordMember nullRolesMember = Substitute.For<IDiscordMember>();
		nullRolesMember.IsBot.Returns(false);
		nullRolesMember.Roles.Returns((IEnumerable<IDiscordRole>?) null);

		// 3. Member does not have the required role
		IDiscordMember noRoleMember = Substitute.For<IDiscordMember>();
		noRoleMember.IsBot.Returns(false);
		noRoleMember.Roles.Returns([]);

		testGuild.GetAllMembersAsync().Returns(Task.FromResult<IReadOnlyCollection<IDiscordMember>>(
			[botMember, nullRolesMember, noRoleMember]
		));

		// Act.
		await _job!.Execute(DateTime.Today);

		// Assert.
		await _messageServcieMock!.DidNotReceiveWithAnyArgs().SendMessage(default, default, default, default);
		_loggerMock!.Received().Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("All nicknames have been reviewed; no changes were necessary")),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task Execute_SendsPrivateMessageAndHandlesError_OnUnauthorizedException()
	{
		// Arrange
		IDiscordChannel testChannel = Substitute.For<IDiscordChannel>();
		IDiscordGuild testGuild = Substitute.For<IDiscordGuild>();
		testChannel.Guild.Returns(testGuild);
		_channelServiceMock!.GetBotTestChannel().Returns(Task.FromResult(testChannel));
		IDiscordRole memberRole = Substitute.For<IDiscordRole>();
		memberRole.Id.Returns(123UL);
		testGuild.GetRole(Arg.Any<ulong>()).Returns(memberRole);

		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.IsBot.Returns(false);
		member.Roles.Returns([memberRole]);
		member.DisplayName.Returns("Player");
		member.Username.Returns("Player");
		testGuild.GetAllMembersAsync().Returns(Task.FromResult<IReadOnlyCollection<IDiscordMember>>([member]));

		_userServiceMock!.GetWotbPlayerNameFromDisplayName(Arg.Any<string>()).Returns(new WotbPlayerNameInfo("", "Player"));
		_wgAcacountServiceMock!.SearchByName(Arg.Any<SearchAccuracy>(), Arg.Any<string>(), Arg.Any<string>(), false, true, false)
			.Returns(Task.FromResult<IReadOnlyList<IWGAccount>>(
			[
			Substitute.For<IWGAccount>()
			]));

		// Simulate UnauthorizedException when changing nickname.
		_userServiceMock!
			.When(x => x.ChangeMemberNickname(Arg.Any<IDiscordMember>(), Arg.Any<string>()))
			.Do(_ => { throw new UnauthorizedAccessException(); });

		// Act.
		await _job!.Execute(DateTime.UtcNow);

		// Assert.
		await _messageServcieMock!.Received(1).SendPrivateMessage(member, testGuild.Name, Arg.Any<string>());
		await _errorHandlerMock!.Received(1).HandleErrorAsync(
			Arg.Is<string>(msg => msg.Contains("Failed to change nickname for user")),
			Arg.Any<UnauthorizedAccessException>()
		);
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new VerifyServerNicknamesJob(null, _channelServiceMock, _messageServcieMock, _wgAcacountServiceMock, _errorHandlerMock, _optionsMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, null, _messageServcieMock, _wgAcacountServiceMock, _errorHandlerMock, _optionsMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, null, _wgAcacountServiceMock, _errorHandlerMock, _optionsMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, _messageServcieMock, null, _errorHandlerMock, _optionsMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, _messageServcieMock, _wgAcacountServiceMock, null, _optionsMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, _messageServcieMock, _wgAcacountServiceMock, _errorHandlerMock, null, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, _messageServcieMock, _wgAcacountServiceMock, _errorHandlerMock, _optionsMock, null, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock, _channelServiceMock, _messageServcieMock, _wgAcacountServiceMock, _errorHandlerMock, _optionsMock, _botStateMock, null));
	}
}
