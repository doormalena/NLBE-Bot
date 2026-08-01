namespace NLBE_Bot.Tests.Services;

using DSharpPlus.Exceptions;
using DSharpPlus.Net.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using NLBE_Bot.Services;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

[TestClass]
public class UserServiceTests
{
	private IMessageService? _messageServiceMock;
	private ILogger<UserService>? _loggerMock;
	private IAccountsRepository? _accountsRepositoryMock;
	private IClansRepository? _clansRepositoryMock;
	private UserService? _userService;
	private IDiscordGuild? _guildMock;
	private IDiscordMember? _memberMock;
	private IDiscordChannel? _channelMock;

	[TestInitialize]
	public void Setup()
	{
		_messageServiceMock = Substitute.For<IMessageService>();
		_loggerMock = Substitute.For<ILogger<UserService>>();
		_accountsRepositoryMock = Substitute.For<IAccountsRepository>();
		_clansRepositoryMock = Substitute.For<IClansRepository>();
		_guildMock = Substitute.For<IDiscordGuild>();
		_memberMock = Substitute.For<IDiscordMember>();
		_channelMock = Substitute.For<IDiscordChannel>();
		_userService = new UserService(_loggerMock, _messageServiceMock, _accountsRepositoryMock, _clansRepositoryMock);
	}

	[TestMethod]
	[DataRow("[TAG] PlayerName", "[TAG]", "PlayerName")]
	[DataRow("[NLBE] JohnDoe", "[NLBE]", "JohnDoe")]
	[DataRow("[] NoClanTagName", "[]", "NoClanTagName")]
	[DataRow("NoClanTagName", "", "NoClanTagName")]
	[DataRow("[CLAN] Name With Spaces", "[CLAN]", "Name With Spaces")]
	[DataRow("[CLAN]NameNoSpace", "[CLAN]", "NameNoSpace")]
	[DataRow("[CLAN] ", "[CLAN]", "")]
	[DataRow("", "", "")]
	public void GetWotbPlayerNameFromDisplayName_ParsesCorrectly(string displayName, string expectedClanTag, string expectedPlayerName)
	{
		// Act.
		WotbPlayerNameInfo result = _userService!.GetWotbPlayerNameFromDisplayName(displayName);

		// Assert.
		Assert.AreEqual(expectedClanTag, result.ClanTag);
		Assert.AreEqual(expectedPlayerName, result.PlayerName);
	}

	#region GetDiscordMember Tests

	[TestMethod]
	public async Task GetDiscordMember_ReturnsNull_WhenMemberNotFound()
	{
		// Arrange.
		ulong userId = 123456789;
		_guildMock!.GetMemberAsync(userId).Returns(Task.FromResult((IDiscordMember?) null));

		// Act.
		IDiscordMember? result = await _userService!.GetDiscordMember(_guildMock, userId);

		// Assert.
		Assert.IsNull(result);
		await _guildMock.Received(1).GetMemberAsync(userId);
	}

	[TestMethod]
	public async Task GetDiscordMember_ReturnsMember_WhenMemberExists()
	{
		// Arrange.
		ulong userId = 123456789;
		_guildMock!.GetMemberAsync(userId).Returns(Task.FromResult(_memberMock));

		// Act.
		IDiscordMember? result = await _userService!.GetDiscordMember(_guildMock, userId);

		// Assert.
		Assert.IsNotNull(result);
		Assert.AreEqual(_memberMock, result);
		await _guildMock.Received(1).GetMemberAsync(userId);
	}

	#endregion

	#region ChangeMemberNickname Tests

	[TestMethod]
	public async Task ChangeMemberNickname_UpdatesNickname_OnSuccess()
	{
		// Arrange.
		string newNickname = "NewNickname";

		// Act.
		await _userService!.ChangeMemberNickname(_memberMock!, newNickname);

		// Assert.
		await _memberMock!.Received(1).ModifyAsync(Arg.Any<Action<MemberEditModel>>());
	}

	[TestMethod]
	public async Task ChangeMemberNickname_ThrowsUnauthorizedAccessException_WhenUnauthorized()
	{
		// Arrange.
		string newNickname = "NewNickname";
		UnauthorizedAccessException unauthorizedException = new("Unauthorized");
		_memberMock!.ModifyAsync(Arg.Any<Action<MemberEditModel>>())
			.Returns(Task.FromException(unauthorizedException));

		// Act & Assert.
		UnauthorizedAccessException thrownException = await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
			() => _userService!.ChangeMemberNickname(_memberMock, newNickname));

		Assert.IsNotNull(thrownException);
		Assert.IsTrue(thrownException.Message.Contains("insufficient permissions"));
	}

	[TestMethod]
	public async Task ChangeMemberNickname_LogsError_WhenGenericExceptionOccurs()
	{
		// Arrange.
		string newNickname = "NewNickname";
		Exception exception = new("Test error");
		_memberMock!.ModifyAsync(Arg.Any<Action<MemberEditModel>>())
			.Returns(Task.FromException(exception));
		_memberMock.Username.Returns("TestUser");

		// Act.
		await _userService!.ChangeMemberNickname(_memberMock, newNickname);

		// Assert.
		_loggerMock!.Received(1).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Is<Exception>(e => e == exception),
			Arg.Any<Func<object, Exception?, string>>());
	}

	#endregion

	#region UpdateName Tests

	[TestMethod]
	[DataRow("SimpleName", "SimpleName")]
	[DataRow("Name With Spaces", "Name With Spaces")]
	public void UpdateName_ReturnsUnchangedName_WhenNoSpecialCharacters(string displayName, string expected)
	{
		// Arrange.
		_memberMock!.Roles.Returns([]);

		// Act.
		string result = _userService!.UpdateName(_memberMock, displayName);

		// Assert.
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void UpdateName_RemovesTrailingSpaces_FromName()
	{
		// Arrange.
		string displayName = "TestName   ";
		_memberMock!.Roles.Returns([]);

		// Act.
		string result = _userService!.UpdateName(_memberMock, displayName);

		// Assert.
		Assert.IsFalse(result.EndsWith(" "));
	}

	#endregion

	#region ShowMemberInfo Tests

	[TestMethod]
	public async Task ShowMemberInfo_CreatesEmbed_WhenDiscordMemberProvided()
	{
		// Arrange.
		_memberMock!.Username.Returns("TestUser");
		_memberMock.Discriminator.Returns("0001");
		_memberMock.DisplayName.Returns("TestUser#0001");
		_memberMock.AvatarUrl.Returns("https://example.com/avatar.png");
		_memberMock.Id.Returns(123456789UL);
		_memberMock.IsBot.Returns(false);
		_memberMock.Roles.Returns([]);
		_memberMock.JoinedAt.Returns(DateTimeOffset.Now);
		_memberMock.Presence.Returns(null as DSharpPlus.Entities.DiscordPresence);
		_memberMock.Verified.Returns(true);

		// Act.
		await _userService!.ShowMemberInfo(_channelMock!, _memberMock);

		// Assert.
		await _messageServiceMock!.Received(1).CreateEmbed(_channelMock!, Arg.Any<EmbedOptions>());
	}

	[TestMethod]
	public async Task ShowMemberInfo_CreatesEmbed_WhenWotbAccountInfoProvided()
	{
		// Arrange.
		WotbAccountInfo accountInfo = new()
		{
			AccountId = 12345,
			Nickname = "TestPlayer",
			CreatedAt = DateTime.Now,
			LastBattleTime = DateTime.Now,
			Statistics = new()
			{
				Rating = new()
				{
					Wins = 1,
					Battles = 1
				},
				All = new()
				{
					Wins = 1,
					Battles = 1,
					DamageDealt = 100
				}
			}
		};
		_clansRepositoryMock!.GetAccountClanInfoAsync(12345)
			.Returns(Task.FromResult((WotbAccountClanInfo?) null));

		// Act.
		await _userService!.ShowMemberInfo(_channelMock!, accountInfo);

		// Assert.
		await _messageServiceMock!.Received(1).CreateEmbed(_channelMock!, Arg.Any<EmbedOptions>());
	}

	[TestMethod]
	public async Task ShowMemberInfo_DoesNotCreateEmbed_WhenNeitherDiscordMemberNorWotbAccount()
	{
		// Act.
		await _userService!.ShowMemberInfo(_channelMock!, "InvalidInput");

		// Assert.
		await _messageServiceMock!.DidNotReceive().CreateEmbed(_channelMock!, Arg.Any<EmbedOptions>());
	}

	#endregion

	#region ListInPlayerEmbed Tests

	[TestMethod]
	public void ListInPlayerEmbed_ReturnsEmptyList_WhenNoMembersProvided()
	{
		// Arrange.
		List<WotbClanMember> memberList = [];

		// Act.
		List<DEF> result = _userService!.ListInPlayerEmbed(2, memberList, "search", _guildMock!).Result;

		// Assert.
		Assert.AreEqual(0, result.Count);
	}

	[TestMethod]
	public void ListInPlayerEmbed_FiltersMembers_BySearchTerm()
	{
		// Arrange.
		List<WotbClanMember> memberList =
		[
			new() { AccountId = 1, AccountName = "Player1", Role = "commander" },
			new() { AccountId = 2, AccountName = "AnotherPlayer", Role = "officer" },
			new() { AccountId = 3, AccountName = "Player2", Role = "member" }
		];

		// Act.
		List<DEF> result = _userService!.ListInPlayerEmbed(2, memberList, "Player", _guildMock!).Result;

		// Assert.
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public void ListInPlayerEmbed_ReturnsMultipleColumns_WhenColumnCountGreaterThanOne()
	{
		// Arrange.
		List<WotbClanMember> memberList =
		[
			new() { AccountId = 1, AccountName = "Player1", Role = "commander" },
			new() { AccountId = 2, AccountName = "Player2", Role = "officer" },
			new() { AccountId = 3, AccountName = "Player3", Role = "member" }
		];

		// Act.
		List<DEF> result = _userService!.ListInPlayerEmbed(2, memberList, string.Empty, _guildMock!).Result;

		// Assert.
		Assert.IsNotNull(result);
	}

	#endregion

	#region ListInMemberEmbed Tests

	[TestMethod]
	public void ListInMemberEmbed_ReturnsEmptyList_WhenNoMembersProvided()
	{
		// Arrange.
		List<IDiscordMember> memberList = [];

		// Act.
		List<DEF> result = _userService!.ListInMemberEmbed(2, memberList, "search");

		// Assert.
		Assert.AreEqual(0, result.Count);
	}

	[TestMethod]
	public void ListInMemberEmbed_FiltersMembers_BySearchTerm()
	{
		// Arrange.
		IDiscordMember member1 = Substitute.For<IDiscordMember>();
		member1.DisplayName.Returns("Player1");
		IDiscordMember member2 = Substitute.For<IDiscordMember>();
		member2.DisplayName.Returns("AnotherUser");
		IDiscordMember member3 = Substitute.For<IDiscordMember>();
		member3.DisplayName.Returns("Player2");

		List<IDiscordMember> memberList = [member1, member2, member3];

		// Act.
		List<DEF> result = _userService!.ListInMemberEmbed(2, memberList, "Player");

		// Assert.
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public void ListInMemberEmbed_ReturnsMultipleColumns_WhenColumnCountGreaterThanOne()
	{
		// Arrange.
		IDiscordMember member1 = Substitute.For<IDiscordMember>();
		member1.DisplayName.Returns("Player1");
		IDiscordMember member2 = Substitute.For<IDiscordMember>();
		member2.DisplayName.Returns("Player2");
		IDiscordMember member3 = Substitute.For<IDiscordMember>();
		member3.DisplayName.Returns("Player3");

		List<IDiscordMember> memberList = [member1, member2, member3];

		// Act.
		List<DEF> result = _userService!.ListInMemberEmbed(2, memberList, string.Empty);

		// Assert.
		Assert.IsNotNull(result);
	}

	#endregion

	#region SearchPlayer Tests

	[TestMethod]
	public async Task SearchPlayer_ReturnsNull_WhenPlayerNotFound()
	{
		// Arrange.
		IDiscordUser user = Substitute.For<IDiscordUser>();
		_accountsRepositoryMock!.SearchByNameAsync(SearchType.StartsWith, "NonExistentPlayer", 20)
			.Returns(Task.FromResult<IReadOnlyList<WotbAccountListItem>>([]));

		// Act.
		WotbAccountInfo? result = await _userService!.SearchPlayer(_channelMock!, _memberMock!, user, "Guild", "NonExistentPlayer");

		// Assert.
		Assert.IsNull(result);
	}

	[TestMethod]
	public async Task SearchPlayer_ReturnsAccount_WhenSinglePlayerFound()
	{
		// Arrange.
		IDiscordUser user = Substitute.For<IDiscordUser>();
		WotbAccountInfo accountInfo = new()
		{
			AccountId = 12345,
			Nickname = "TestPlayer",
			Statistics = new WotbAccountStatistics
			{
				Rating = new()
				{
					Wins = 1,
					Battles = 1
				},
				All = new()
				{
					Wins = 1,
					Battles = 1,
					DamageDealt = 100
				}
			}
		};
		List<WotbAccountListItem> searchResults = [accountInfo];
		_accountsRepositoryMock!.SearchByNameAsync(SearchType.StartsWith, "TestPlayer", 20)
			.Returns(Task.FromResult<IReadOnlyList<WotbAccountListItem>>(searchResults));
		_accountsRepositoryMock!.GetByIdAsync(12345).Returns(Task.FromResult((WotbAccountInfo?) accountInfo));

		// Act.
		WotbAccountInfo? result = await _userService!.SearchPlayer(_channelMock!, _memberMock!, user, "Guild", "TestPlayer");

		// Assert.
		Assert.IsNotNull(result);
		Assert.AreEqual("TestPlayer", result.Nickname);
	}

	#endregion
}
