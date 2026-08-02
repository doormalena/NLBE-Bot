namespace WorldOfTanksBlitzApi.Tests.Repositories;

using NSubstitute;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;
using WorldOfTanksBlitzApi.Repositories;

[TestClass]
public class AchievementsRepositoryTests
{
	private IWotbConnection? _connectionMock;
	private AchievementsRepository? _repository;
	private IWotInspectorAchievementMappingProvider? _mappingProviderMock;

	[TestInitialize]
	public void Setup()
	{
		_connectionMock = Substitute.For<IWotbConnection>();
		_mappingProviderMock = Substitute.For<IWotInspectorAchievementMappingProvider>();
		_repository = new AchievementsRepository(_connectionMock, _mappingProviderMock);
	}

	[TestMethod]
	public async Task GetAllAsync_ReturnsAchievements_WithWotInspectorId()
	{
		// Arrange.
		string achievementsJson = """
        {
            "achievement1": {
                "achievement_id": "achievement1",
                "condition": "condition1",
                "name": "Achievement One",
                "description": "Desc",
                "image": "img.png",
                "image_big": "img_big.png",
                "order": 1,
                "section": "section1",
                "options": null
            },
            "achievement2": {
                "achievement_id": "achievement2",
                "condition": "condition2",
                "name": "Achievement Two",
                "description": "Desc2",
                "image": "img2.png",
                "image_big": "img2_big.png",
                "order": 2,
                "section": "section2",
                "options": null
            }
        }
        """;

		string apiResponse = $"{{\"data\": {achievementsJson} }}";

		_connectionMock!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>())
			.Returns(Task.FromResult(apiResponse));
		_mappingProviderMock!.GetMappingAsync().Returns(Task.FromResult<Dictionary<string, string>?>(new Dictionary<string, string>
		{
			{ "601", "achievement1" },
			{ "602", "achievement2" }
		}));

		// Act.
		Dictionary<string, WotbAchievement>? achievements = await _repository!.GetAllAsync();

		// Assert.
		Assert.IsNotNull(achievements);
		Assert.AreEqual(2, achievements.Count);
		Assert.IsTrue(achievements.ContainsKey("achievement1"));
		Assert.IsTrue(achievements.ContainsKey("achievement2"));
		Assert.AreEqual(601, achievements["achievement1"].WotInspectorId);
		Assert.AreEqual(602, achievements["achievement2"].WotInspectorId);
	}

	[TestMethod]
	public async Task GetAllAsync_ReturnsNull_WhenApiReturnsNullData()
	{
		// Arrange.
		string apiResponse = "{\"data\": null}";

		_connectionMock!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>())
			.Returns(Task.FromResult(apiResponse));

		// Act.
		Dictionary<string, WotbAchievement>? achievements = await _repository!.GetAllAsync();

		// Assert.
		Assert.IsNull(achievements);
	}

	[TestMethod]
	public async Task GetAllAsync_HandlesEmptyMapping()
	{
		// Arrange.
		string achievementsJson = """
        {
            "achievement1": {
                "achievement_id": "achievement1",
                "name": "Achievement One",
                "description": "Desc",
                "image": "img.png",
                "image_big": "img_big.png",
                "order": 1,
                "section": "section1",
                "options": null
            }
        }
        """;

		string apiResponse = $"{{\"data\": {achievementsJson} }}";

		_connectionMock!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>())
			.Returns(Task.FromResult(apiResponse));

		// Act.
		Dictionary<string, WotbAchievement>? achievements = await _repository!.GetAllAsync();

		// Assert.
		Assert.IsNotNull(achievements);
		Assert.AreEqual(1, achievements.Count);
		Assert.AreEqual(0, achievements["achievement1"].WotInspectorId); // Default value if not mapped
	}
}
