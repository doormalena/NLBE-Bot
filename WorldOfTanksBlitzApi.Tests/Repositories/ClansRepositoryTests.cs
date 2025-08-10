namespace WorldOfTanksBlitzApi.Tests.Repositories;

using NSubstitute;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;
using WorldOfTanksBlitzApi.Repositories;

[TestClass]
public class ClansRepositoryTests
{
	private IWotbConnection? _mockConnection;
	private ClansRepository? _repository;

	[TestInitialize]
	public void Setup()
	{
		_mockConnection = Substitute.For<IWotbConnection>();
		_repository = new ClansRepository(_mockConnection);
	}

	[TestMethod]
	public async Task SearchByNameAsync_ReturnsList_WhenDataIsPresent()
	{
		// Arrange.
		string json = "{\"data\":[{\"name\":\"ClanTest\",\"clan_id\":456}]}";
		_mockConnection!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>()).Returns(json);

		// Act.
		IReadOnlyList<WotbClanListItem> result = await _repository!.SearchByNameAsync(SearchType.Exact, "ClanTest");

		// Assert.
		Assert.AreEqual(1, result.Count);
		Assert.AreEqual("ClanTest", result[0].Name);
		Assert.AreEqual(456, result[0].ClanId);
	}

	[TestMethod]
	public async Task SearchByNameAsync_ReturnsEmpty_WhenDataIsNull()
	{
		// Arrange.
		string json = "{\"data\":null}";
		_mockConnection!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>()).Returns(json);

		// Act.
		IReadOnlyList<WotbClanListItem> result = await _repository!.SearchByNameAsync(SearchType.Exact, "ClanTest");

		// Assert.
		Assert.AreEqual(0, result.Count);
	}

	[TestMethod]
	public async Task GetByIdAsync_ReturnsClanInfo_WhenDataIsPresent()
	{
		// Arrange.
		string json = @"
		{
			""data"": {
				""456"": {
					""name"": ""ClanTest"",
					""clan_id"": 456,
					""tag"": ""CT"",
					""members_count"": 3,
					""created_at"": 1438763237,
					""members_ids"": [12345,67890,34619],
					""motto"": ""Victory or nothing!"",
					""description"": ""A test clan."",
					""members"": [
						{ ""account_id"": 12345, ""role"": ""commander"", ""joined_at"": 1706558549 },
						{ ""account_id"": 67890, ""role"": ""private"", ""joined_at"": 1504631065 },
						{ ""account_id"": 34619, ""role"": ""executive_officer"", ""joined_at"": 1534449607 }
					]
				}
			}
		}";
		_mockConnection!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>()).Returns(json);

		// Act.
		WotbClanInfo? result = await _repository!.GetByIdAsync(456);

		// Assert.
		Assert.IsNotNull(result);
		Assert.AreEqual("ClanTest", result.Name);
		Assert.AreEqual(456, result.ClanId);
		Assert.AreEqual("CT", result.Tag);
		Assert.AreEqual(3, result.MembersCount);
		Assert.AreEqual(new DateTime(2015, 8, 5, 8, 27, 17, DateTimeKind.Utc), result.CreatedAt);
		CollectionAssert.AreEqual(new List<int> { 12345, 67890, 34619 }, result.MemberIds);
		Assert.AreEqual("Victory or nothing!", result.Motto);
		Assert.AreEqual("A test clan.", result.Description);
		Assert.IsNotNull(result.Members);
		Assert.AreEqual(3, result.Members.Count);
		Assert.AreEqual(12345, result.Members[0].AccountId);
		Assert.AreEqual("commander", result.Members[0].Role);
		Assert.AreEqual(new DateTime(2024, 1, 29, 20, 02, 29, DateTimeKind.Utc), result.Members[0].JoinedAt);
		Assert.AreEqual(67890, result.Members[1].AccountId);
		Assert.AreEqual("private", result.Members[1].Role);
		Assert.AreEqual(new DateTime(2017, 9, 5, 17, 04, 25, DateTimeKind.Utc), result.Members[1].JoinedAt);
		Assert.AreEqual(34619, result.Members[2].AccountId);
		Assert.AreEqual("executive_officer", result.Members[2].Role);
		Assert.AreEqual(new DateTime(2018, 8, 16, 20, 0, 7, DateTimeKind.Utc), result.Members[2].JoinedAt);
	}

	[TestMethod]
	public async Task GetByIdAsync_ReturnsNull_WhenDataIsMissing()
	{
		// Arrange.
		string json = "{\"data\":{}}";
		_mockConnection!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>()).Returns(json);

		// Act.
		WotbClanInfo? result = await _repository!.GetByIdAsync(456);

		// Assert.
		Assert.IsNull(result);
	}

	[TestMethod]
	public async Task GetAccountClanInfoAsync_ReturnsAccountClanInfo_WhenDataIsPresent()
	{
		// Arrange.
		string json = @"
		{
			""data"": {
				""789"": {
					""account_id"": 789,
					""account_name"": ""Player789"",
					""joined_at"": 1700000000,
					""role"": ""commander"",
					""clan_id"": 456,
					""clan"": {
						""name"": ""ClanTest"",
						""clan_id"": 456,
						""tag"": ""CT"",
						""members_count"": 10,
						""created_at"": 1577836800						
					}
				}
			}
		}";
		_mockConnection!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>()).Returns(json);

		// Act.
		WotbAccountClanInfo? result = await _repository!.GetAccountClanInfoAsync(789);

		// Assert.
		Assert.IsNotNull(result);
		Assert.AreEqual(789, result.AccountId);
		Assert.AreEqual("Player789", result.AccountName);
		Assert.AreEqual(new DateTime(2023, 11, 14, 22, 13, 20, DateTimeKind.Utc), result.JoinedAt);
		Assert.AreEqual("commander", result.Role);
		Assert.AreEqual(456, result.ClanId);
		Assert.IsNotNull(result.Clan);
		Assert.AreEqual("ClanTest", result.Clan.Name);
		Assert.AreEqual(456, result.Clan.ClanId);
		Assert.AreEqual("CT", result.Clan.Tag);
		Assert.AreEqual(10, result.Clan.MembersCount);
		Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.Clan.CreatedAt);
	}

	[TestMethod]
	public async Task GetAccountClanInfoAsync_ReturnsNull_WhenDataIsMissing()
	{
		// Arrange.
		string json = @"{ ""data"": {} }";
		_mockConnection!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>()).Returns(json);

		// Act.
		WotbAccountClanInfo? result = await _repository!.GetAccountClanInfoAsync(789);

		// Assert.
		Assert.IsNull(result);
	}

	[TestMethod]
	public async Task GetAccountClanInfoAsync_ReturnsNull_WhenAccountClanNodeIsNull()
	{
		// Arrange.
		string json = @"{ ""data"": { ""789"": null } }";
		_mockConnection!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>()).Returns(json);

		// Act.
		WotbAccountClanInfo? result = await _repository!.GetAccountClanInfoAsync(789);

		// Assert.
		Assert.IsNull(result);
	}
}

