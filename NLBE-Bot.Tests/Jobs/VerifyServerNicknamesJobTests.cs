namespace NLBE_Bot.Tests.Jobs;

using DSharpPlus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Jobs;
using NLBE_Bot.Models;
using NSubstitute;
using System;
using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

[TestClass]
public class VerifyServerNicknamesJobTests
{
	private IUserService? _userServiceMock;
	private IMessageService? _messageServcieMock;
	private IAccountsRepository? _accountsRepositoryMock;
	private IClansRepository? _clansRepositoryMock;
	private IBotState? _botStateMock;
	private IOptions<BotOptions>? _optionsMock;
	private ILogger<VerifyServerNicknamesJob>? _loggerMock;
	private IDiscordGuild? _guildMock;
	private VerifyServerNicknamesJob? _job;

	[TestInitialize]
	public void Setup()
	{
		_optionsMock = Substitute.For<IOptions<BotOptions>>();
		_optionsMock.Value.Returns(new BotOptions()
		{
			ChannelIds = new()
			{
				BotTest = 1234
			},
			RoleIds = new()
			{
				Members = 1234567890
			}
		});
		_userServiceMock = Substitute.For<IUserService>();
		_messageServcieMock = Substitute.For<IMessageService>();
		_accountsRepositoryMock = Substitute.For<IAccountsRepository>();
		_clansRepositoryMock = Substitute.For<IClansRepository>();
		_botStateMock = Substitute.For<IBotState>();
		_loggerMock = Substitute.For<ILogger<VerifyServerNicknamesJob>>();
		_guildMock = Substitute.For<IDiscordGuild>();

		_job = new(_userServiceMock, _messageServcieMock, _accountsRepositoryMock, _clansRepositoryMock, _optionsMock, _botStateMock, _loggerMock);
	}

	[TestMethod]
	public async Task Execute_WhenNotVerifiedToday()
	{
		// Arrange.
		DateTime now = DateTime.Now;
		DateTime yesterday = DateTime.Now.AddDays(-1);
		_botStateMock!.LasTimeServerNicknamesWereVerified.Returns(yesterday);

		// Act.
		await _job!.Execute(_guildMock!, now);

		// Assert.
		_guildMock!.Received(1).GetChannel(_optionsMock!.Value.ChannelIds.BotTest);
		Assert.AreEqual(_botStateMock!.LasTimeServerNicknamesWereVerified, now);
	}

	[TestMethod]
	public async Task Execute_HandlesException()
	{
		// Arrange.
		DateTime yesterday = DateTime.Now.AddDays(-1);
		DateTime now = DateTime.Now;
		Exception ex = new("fail");
		_botStateMock!.LasTimeServerNicknamesWereVerified.Returns(yesterday);
		_guildMock!.GetChannel(_optionsMock!.Value.ChannelIds.BotTest).Returns(x => throw ex);

		// Act.
		await _job!.Execute(_guildMock!, now);

		// Assert.
		_loggerMock!.Received().Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v.ToString()!.Contains("An error occured while verifing all server nicknames.")),
			ex,
			Arg.Any<Func<object, Exception?, string>>());
		Assert.AreEqual(_botStateMock!.LasTimeServerNicknamesWereVerified, yesterday);
	}

	[TestMethod]
	public async Task Execute_DoesNothing_WhenAlreadyVerifiedToday()
	{
		// Arrange.
		_botStateMock!.LasTimeServerNicknamesWereVerified.Returns(DateTime.Today);

		// Act.
		await _job!.Execute(_guildMock!, DateTime.Today);

		// Assert
		_botStateMock.DidNotReceive().LasTimeServerNicknamesWereVerified = Arg.Any<DateTime>();
	}

	[TestMethod]
	public async Task Execute_LogsWarning_WhenChannelIsNull()
	{
		// Arrange.
		_guildMock!.GetChannel(_optionsMock!.Value.ChannelIds.BotTest).Returns((IDiscordChannel?) null);

		// Act.
		await _job!.Execute(_guildMock!, DateTime.Today);

		// Assert.
		_loggerMock!.Received().Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("Bot Test channel is missing")),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task Execute_LogsWarning_WhenDefaultMemberRoleIsNull()
	{
		// Arrange.
		IDiscordChannel testChannel = Substitute.For<IDiscordChannel>();
		_guildMock!.GetChannel(_optionsMock!.Value.ChannelIds.BotTest).Returns(testChannel);
		_guildMock!.GetRole(Arg.Any<ulong>()).Returns((IDiscordRole?) null);

		// Act.
		await _job!.Execute(_guildMock!, DateTime.UtcNow);

		// Assert.
		_loggerMock!.Received().Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains($"Default member role with id `{_optionsMock!.Value.RoleIds.Members}` is missing")),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
		await _guildMock.DidNotReceive().GetAllMembersAsync();
	}

	[TestMethod]
	public async Task Execute_SendsNoChangesMessage_WhenNoInvalidMatches()
	{
		// Arrange,
		IDiscordChannel channelMock = Substitute.For<IDiscordChannel>();
		_guildMock!.GetChannel(_optionsMock!.Value.ChannelIds.BotTest).Returns(channelMock);

		IDiscordRole memberRole = Substitute.For<IDiscordRole>();
		_guildMock!.GetRole(Arg.Any<ulong>()).Returns(memberRole);

		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.IsBot.Returns(false);
		member.Roles.Returns([memberRole]);
		member.DisplayName.Returns("[NLBE] Player");
		member.Username.Returns("Player");
		_guildMock!.GetAllMembersAsync().Returns([member]);

		_userServiceMock!.GetWotbPlayerNameFromDisplayName(Arg.Any<string>()).Returns(new WotbPlayerNameInfo("[NLBE]", "Player"));

		WotbAccountInfo accountInfo = new()
		{
			Nickname = "Player",
			AccountId = 12345
		};
		WotbAccountClanInfo accountClanInfo = new()
		{
			AccountId = accountInfo.AccountId,
			AccountName = accountInfo.Nickname,
			ClanId = 67890,
			Clan = new WotbClanInfo
			{
				ClanId = 67890,
				Tag = "NLBE"
			}
		};
		_accountsRepositoryMock!.SearchByNameAsync(SearchType.Exact, accountInfo.Nickname, 1)
			.Returns(Task.FromResult<IReadOnlyList<WotbAccountListItem>>([accountInfo]));
		_clansRepositoryMock!.GetAccountClanInfoAsync(accountInfo.AccountId).Returns(Task.FromResult<WotbAccountClanInfo?>(accountClanInfo));

		// Act.
		await _job!.Execute(_guildMock!, DateTime.Today);

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
		IDiscordChannel channelMock = Substitute.For<IDiscordChannel>();
		_guildMock!.GetChannel(_optionsMock!.Value.ChannelIds.BotTest).Returns(channelMock);

		IDiscordRole memberRole = Substitute.For<IDiscordRole>();
		_guildMock.GetRole(Arg.Any<ulong>()).Returns(memberRole);

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

		_guildMock.GetAllMembersAsync().Returns(Task.FromResult<IReadOnlyCollection<IDiscordMember>>(
			[member1, member2]
		));

		_userServiceMock!.GetWotbPlayerNameFromDisplayName(member1.DisplayName).Returns(new WotbPlayerNameInfo("", "Player1"));
		_userServiceMock!.GetWotbPlayerNameFromDisplayName(member2.DisplayName).Returns(new WotbPlayerNameInfo("[NLBE2]", "Player2"));

		WotbAccountInfo accountInfo1 = new()
		{
			Nickname = "Player1",
			AccountId = 12345
		};
		WotbAccountClanInfo accountClanInfo1 = new()
		{
			AccountId = accountInfo1.AccountId,
			AccountName = accountInfo1.Nickname,
			ClanId = 43210,
			Clan = new WotbClanInfo
			{
				ClanId = 67890,
				Tag = "NLBE"
			}
		};
		WotbAccountInfo accountInfo2 = new()
		{
			Nickname = "Player2",
			AccountId = 67890
		};
		WotbAccountClanInfo accountClanInfo2 = new()
		{
			AccountId = accountInfo2.AccountId,
			AccountName = accountInfo2.Nickname,
			ClanId = 98765,
			Clan = new WotbClanInfo
			{
				ClanId = 67890,
				Tag = "TAG"
			}
		};

		_accountsRepositoryMock!.SearchByNameAsync(SearchType.Exact, accountInfo1.Nickname, 1)
			.Returns(Task.FromResult<IReadOnlyList<WotbAccountListItem>>([accountInfo1]));
		_clansRepositoryMock!.GetAccountClanInfoAsync(accountInfo1.AccountId).Returns(Task.FromResult<WotbAccountClanInfo?>(accountClanInfo1));
		_accountsRepositoryMock!.SearchByNameAsync(SearchType.Exact, accountInfo2.Nickname, 1)
			.Returns(Task.FromResult<IReadOnlyList<WotbAccountListItem>>([accountInfo2]));
		_clansRepositoryMock!.GetAccountClanInfoAsync(accountInfo2.AccountId).Returns(Task.FromResult<WotbAccountClanInfo?>(accountClanInfo2));

		// Act.
		await _job!.Execute(_guildMock!, DateTime.Today);

		// Assert.
		await _userServiceMock!.Received(2).ChangeMemberNickname(
			Arg.Any<IDiscordMember>(),
			Arg.Is<string>(s => s.Contains(accountInfo1.Nickname) || s.Contains(accountInfo2.Nickname))
		);
		await _messageServcieMock!.Received(2).SendMessage(
			channelMock,
			null,
			_guildMock.Name,
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
		IDiscordChannel channelMock = Substitute.For<IDiscordChannel>();
		_guildMock!.GetChannel(_optionsMock!.Value.ChannelIds.BotTest).Returns(channelMock);

		IDiscordRole memberRole = Substitute.For<IDiscordRole>();
		_guildMock.GetRole(Arg.Any<ulong>()).Returns(memberRole);

		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.IsBot.Returns(false);
		member.Roles.Returns([memberRole]);
		member.DisplayName.Returns("[TAG] Player");
		member.Username.Returns("Player");

		_guildMock.GetAllMembersAsync().Returns(Task.FromResult<IReadOnlyCollection<IDiscordMember>>(
			[member]
		));

		_userServiceMock!.GetWotbPlayerNameFromDisplayName(member.DisplayName).Returns(new WotbPlayerNameInfo("[TAG]", "Player"));
		_accountsRepositoryMock!.SearchByNameAsync(SearchType.Exact, "Player")
			.Returns(Task.FromResult<IReadOnlyList<WotbAccountListItem>>([]));

		// Act.
		await _job!.Execute(_guildMock!, DateTime.Today);

		// Assert.
		await _messageServcieMock!.Received(1).SendPrivateMessage(
			member,
			_guildMock.Name,
			Arg.Is<string>(s => s.Contains("Voor iedere gebruiker in de NLBE discord server wordt gecontroleerd of de ingestelde bijnaam overeenkomt met je WoTB spelersnaam.\nHelaas is dit voor jou niet het geval.\nWil je dit aanpassen?"))
		);
		await _messageServcieMock!.Received(1).SendMessage(
			channelMock,
			null,
			_guildMock.Name,
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

		IDiscordChannel channelMock = Substitute.For<IDiscordChannel>();
		_guildMock!.GetChannel(_optionsMock!.Value.ChannelIds.BotTest).Returns(channelMock);

		IDiscordRole memberRole = Substitute.For<IDiscordRole>();
		_guildMock.GetRole(Arg.Any<ulong>()).Returns(memberRole);

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

		_guildMock.GetAllMembersAsync().Returns(Task.FromResult<IReadOnlyCollection<IDiscordMember>>(
			[botMember, nullRolesMember, noRoleMember]
		));

		// Act.
		await _job!.Execute(_guildMock!, DateTime.Today);

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
		// Arrange.
		IDiscordChannel channelMock = Substitute.For<IDiscordChannel>();
		_guildMock!.GetChannel(_optionsMock!.Value.ChannelIds.BotTest).Returns(channelMock);
		IDiscordRole memberRole = Substitute.For<IDiscordRole>();
		memberRole.Id.Returns(123UL);
		_guildMock.GetRole(Arg.Any<ulong>()).Returns(memberRole);

		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.IsBot.Returns(false);
		member.Roles.Returns([memberRole]);
		member.DisplayName.Returns("Player");
		member.Username.Returns("Player");
		_guildMock.GetAllMembersAsync().Returns(Task.FromResult<IReadOnlyCollection<IDiscordMember>>([member]));

		WotbAccountInfo accountInfo = new()
		{
			Nickname = "Player",
			AccountId = 12345,
		};

		_userServiceMock!.GetWotbPlayerNameFromDisplayName(Arg.Any<string>()).Returns(new WotbPlayerNameInfo("", "Player"));
		_accountsRepositoryMock!.SearchByNameAsync(SearchType.Exact, "Player", 1)
			.Returns(Task.FromResult<IReadOnlyList<WotbAccountListItem>>([accountInfo]));
		_accountsRepositoryMock!.GetByIdAsync(accountInfo.AccountId).Returns(Task.FromResult<WotbAccountInfo?>(accountInfo));

		// Simulate UnauthorizedException when changing nickname.
		Exception ex = new UnauthorizedAccessException();
		_userServiceMock!
			.When(x => x.ChangeMemberNickname(Arg.Any<IDiscordMember>(), Arg.Any<string>()))
			.Do(_ => { throw ex; });

		// Act.
		await _job!.Execute(_guildMock!, DateTime.UtcNow);

		// Assert.
		await _messageServcieMock!.Received(1).SendPrivateMessage(member, _guildMock.Name, Arg.Any<string>());
		_loggerMock!.Received().Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v.ToString()!.Contains("Failed to change nickname for user `Player`")),
			ex,
			Arg.Any<Func<object, Exception?, string>>());
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new VerifyServerNicknamesJob(null!, _messageServcieMock!, _accountsRepositoryMock!, _clansRepositoryMock!, _optionsMock!, _botStateMock!, _loggerMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock!, null!, _accountsRepositoryMock!, _clansRepositoryMock!, _optionsMock!, _botStateMock!, _loggerMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock!, _messageServcieMock!, null!, _clansRepositoryMock!, _optionsMock!, _botStateMock!, _loggerMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock!, _messageServcieMock!, _accountsRepositoryMock!, null!, _optionsMock!, _botStateMock!, _loggerMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock!, _messageServcieMock!, _accountsRepositoryMock!, _clansRepositoryMock!, _optionsMock!, null!, _loggerMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new VerifyServerNicknamesJob(_userServiceMock!, _messageServcieMock!, _accountsRepositoryMock!, _clansRepositoryMock!, _optionsMock!, _botStateMock!, null!));
		Assert.ThrowsException<ArgumentNullException>(() =>
				new VerifyServerNicknamesJob(_userServiceMock!, _messageServcieMock!, _accountsRepositoryMock!, _clansRepositoryMock!, _optionsMock!, _botStateMock!, null!));
	}
}
