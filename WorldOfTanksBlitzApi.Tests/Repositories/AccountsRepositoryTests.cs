namespace WorldOfTanksBlitzApi.Tests.Repositories;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;
using WorldOfTanksBlitzApi.Repositories;

[TestClass]
public class AccountsRepositoryTests
{
	private IWotbConnection? _mockConnection;
	private AccountsRepository? _repository;

	[TestInitialize]
	public void Setup()
	{
		_mockConnection = Substitute.For<IWotbConnection>();
		_repository = new AccountsRepository(_mockConnection);
	}

	[TestMethod]
	public async Task SearchByNameAsync_ReturnsList_WhenDataIsPresent()
	{
		// Arrange.
		string json = "{\"data\":[{\"nickname\":\"Test\",\"account_id\":123}]}";
		_mockConnection!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>()).Returns(json);

		// Act.
		IReadOnlyList<WotbAccountListItem> result = await _repository!.SearchByNameAsync(SearchType.Exact, "Test");

		// Assert.
		Assert.AreEqual(1, result.Count);
		Assert.AreEqual("Test", result[0].Nickname);
		Assert.AreEqual(123, result[0].AccountId);
	}

	[TestMethod]
	public async Task SearchByNameAsync_ReturnsEmpty_WhenDataIsNull()
	{
		// Arrange.
		string json = "{\"data\":null}";
		_mockConnection!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>()).Returns(json);

		// Act.
		IReadOnlyList<WotbAccountListItem> result = await _repository!.SearchByNameAsync(SearchType.Exact, "Test");

		// Assert.
		Assert.AreEqual(0, result.Count);
	}

	[TestMethod]
	public async Task GetByIdAsync_ReturnsAccountInfo_WhenDataIsPresent()
	{
		// Arrange.
		string json = @"
		{
			""data"": {
				""123"": {
					""nickname"": ""TestUser"",
					""account_id"": 123,
					""created_at"": 1673218661,
					""last_battle_time"": 1735266208,
					""updated_at"": 1735481708,
					""statistics"": {
						""all"": {
							""battles"": 1000,
							""wins"": 600,
							""damage_dealt"": 112143528
						},
						""rating"": {
							""battles"": 200,
							""wins"": 3500,
							""damage_dealt"": 54957945
						}
					}
				}
			}
		}";
		_mockConnection!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>()).Returns(json);

		// Act.
		WotbAccountInfo? result = await _repository!.GetByIdAsync(123);

		// Assert.
		Assert.IsNotNull(result);
		Assert.AreEqual("TestUser", result.Nickname);
		Assert.AreEqual(123, result.AccountId);
		Assert.AreEqual(new DateTime(2023, 1, 8, 22, 57, 41, DateTimeKind.Utc), result.CreatedAt);
		Assert.AreEqual(new DateTime(2024, 12, 27, 2, 23, 28, DateTimeKind.Utc), result.LastBattleTime);
		Assert.AreEqual(new DateTime(2024, 12, 29, 14, 15, 08, DateTimeKind.Utc), result.UpdatedAt);
		Assert.AreEqual("https://www.blitzstars.com/sigs/123", result.BlitzStars);
		Assert.IsNotNull(result.Statistics);

		// Statistics: All
		Assert.IsNotNull(result.Statistics.All);
		Assert.AreEqual(1000, result.Statistics.All.Battles);
		Assert.AreEqual(600, result.Statistics.All.Wins);
		Assert.AreEqual(112143528, result.Statistics.All.DamageDealt);

		// Statistics: Rating
		Assert.IsNotNull(result.Statistics.Rating);
		Assert.AreEqual(200, result.Statistics.Rating.Battles);
		Assert.AreEqual(3500, result.Statistics.Rating.Wins);
		Assert.AreEqual(54957945, result.Statistics.Rating.DamageDealt);
	}

	[TestMethod]
	public async Task GetByIdAsync_ReturnsNull_WhenDataIsMissing()
	{
		// Arrange.
		string json = "{\"data\":{}}";
		_mockConnection!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>()).Returns(json);

		// Act.
		WotbAccountInfo? result = await _repository!.GetByIdAsync(123);

		// Assert.
		Assert.IsNull(result);
	}
}

