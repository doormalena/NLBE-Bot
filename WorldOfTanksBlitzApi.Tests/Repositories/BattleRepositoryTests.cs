namespace WorldOfTanksBlitzApi.Tests.Repositories;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;
using WorldOfTanksBlitzApi.Repositories;

[TestClass]
public class BattleRepositoryTests
{
	private IWotInspectorConnection? _mockConnection;
	private BattleRepository? _repository;

	[TestInitialize]
	public void Setup()
	{
		_mockConnection = Substitute.For<IWotInspectorConnection>();
		_repository = new BattleRepository(_mockConnection);
	}

	[TestMethod]
	public async Task GetBattle_ReturnsBattle_WhenApiReturnsValidData()
	{
		// Arrange.
		string fileName = "test.wotbreplay";
		byte[] fileContent = [1, 2, 3];
		string title = "Test Battle";
		long accountId = 12345;

		string json = """
        {
          "status": "ok",
          "data": {
            "summary": {
              "title": "Test Battle",
              "player_name": "Player1",
              "map_name": "Map1"
            }
          }
        }
        """;

		_mockConnection!.UploadReplayAsync(Arg.Any<string>(), fileName, fileContent, title, accountId)
			.Returns(Task.FromResult(json));

		// Act
		WotbBattle? result = await _repository!.GetBattle(fileName, fileContent, title, accountId);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual("Test Battle", result.Summary.Title);
		Assert.AreEqual("Player1", result.Summary.PlayerName);
		Assert.AreEqual("Map1", result.Summary.MapName);
	}

	[TestMethod]
	public async Task GetBattle_ReturnsNull_WhenApiReturnsNullData()
	{
		// Arrange
		string fileName = "test.wotbreplay";
		byte[] fileContent = [1, 2, 3];
		string title = "Test Battle";
		long accountId = 12345;

		string json = """
        {
          "status": "ok",
          "data": null
        }
        """;

		_mockConnection!.UploadReplayAsync(Arg.Any<string>(), fileName, fileContent, title, accountId)
			.Returns(Task.FromResult(json));

		// Act
		WotbBattle? result = await _repository!.GetBattle(fileName, fileContent, title, accountId);

		// Assert
		Assert.IsNull(result);
	}

	[TestMethod]
	public async Task GetBattle_ReturnsNull_WhenApiReturnsNoDataProperty()
	{
		// Arrange
		string fileName = "test.wotbreplay";
		byte[] fileContent = new byte[] { 1, 2, 3 };
		string title = "Test Battle";
		long accountId = 12345;

		string json = """
        {
          "status": "ok"
        }
        """;

		_mockConnection!.UploadReplayAsync(Arg.Any<string>(), fileName, fileContent, title, accountId)
			.Returns(Task.FromResult(json));

		// Act
		WotbBattle? result = await _repository!.GetBattle(fileName, fileContent, title, accountId);

		// Assert
		Assert.IsNull(result);
	}

	[TestMethod]
	public async Task GetBattle_ThrowsException_WhenConnectionThrows()
	{
		// Arrange
		string fileName = "test.wotbreplay";
		byte[] fileContent = new byte[] { 1, 2, 3 };
		string title = "Test Battle";
		long accountId = 12345;

		_mockConnection!.UploadReplayAsync(Arg.Any<string>(), fileName, fileContent, title, accountId)
			.Returns<Task<string>>(x => throw new Exception("API error"));

		// Act & Assert
		await Assert.ThrowsExceptionAsync<Exception>(async () =>
		{
			await _repository!.GetBattle(fileName, fileContent, title, accountId);
		});
	}
}

