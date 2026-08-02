namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

[TestClass]
public class ReplayServiceTests
{
	private ILogger<ReplayService>? _loggerMock;
	private IWeeklyEventService? _weeklyEventServiceMock;
	private IBattleRepository? _battleRepositoryMock;
	private IAchievementsRepository? _achievementsRepositoryMock;
	private IVehiclesRepository? _vehiclesRepositoryMock;
	private IClansRepository? _clansRepositoryMock;
	private IMapService? _mapServiceMock;
	private IAttachmentService? _discordAttachmentServiceMock;
	private WotInspectorBattle? _battle;
	private IDiscordGuild? _guildMock;
	private IMemoryCache? _cacheMock;

	private ReplayService? _replayService;

	[TestInitialize]
	public void Setup()
	{
		_loggerMock = Substitute.For<ILogger<ReplayService>>();
		_weeklyEventServiceMock = Substitute.For<IWeeklyEventService>();
		_battleRepositoryMock = Substitute.For<IBattleRepository>();
		_achievementsRepositoryMock = Substitute.For<IAchievementsRepository>();
		_vehiclesRepositoryMock = Substitute.For<IVehiclesRepository>();
		_clansRepositoryMock = Substitute.For<IClansRepository>();
		_mapServiceMock = Substitute.For<IMapService>();
		_discordAttachmentServiceMock = Substitute.For<IAttachmentService>();
		_guildMock = Substitute.For<IDiscordGuild>();
		_cacheMock = Substitute.For<IMemoryCache>();

		_replayService = new ReplayService(
			_loggerMock,
			_weeklyEventServiceMock,
			_battleRepositoryMock,
			_achievementsRepositoryMock,
			_vehiclesRepositoryMock,
			_clansRepositoryMock,
			_mapServiceMock,
			_discordAttachmentServiceMock,
			_cacheMock
		);
		int vehicleId = 12345;
		_battle = new()
		{
			Title = "Test Battle",
			DetailsUrl = "http://example.com/replay.wotbreplay",
			Protagonist = 12345,
			BattleStartTime = new DateTime(2025, 9, 17, 12, 0, 0, DateTimeKind.Local),
			VehicleDescr = vehicleId,
			PlayersData =
			[
				new WotInspectorPlayerData
				{
					DbId = 12345,
					Team = 1,
					VehicleDescr = vehicleId,
					DamageMade = 1500
				}
			]
		};
		_vehiclesRepositoryMock!.GetByIdAsync(vehicleId).Returns(new WotbVehicle { TankId = vehicleId, Name = "TankName", Tier = 7 });
		_clansRepositoryMock!.GetAccountClanInfoAsync(_battle.Protagonist).Returns(new WotbAccountClanInfo { Clan = new WotbClanInfo { Tag = "CLAN" } });
		_mapServiceMock!.GetAllMaps(_guildMock!).Returns(new Dictionary<string, MapInfo> { { _battle.MapId.ToString(), new MapInfo { Name = "MapName" } } });
	}

	[TestMethod]
	public async Task GetDescriptionForReplay_ShouldIncludeWeeklyEventAndReplayInfo()
	{
		// Arrange.
		_weeklyEventServiceMock!.GetStringForWeeklyEvent(_guildMock!, _battle!).Returns("Weekly Event Info");

		// Act.
		string result = await _replayService!.GetDescriptionForReplay(_guildMock!, _battle!, 1, "Pre-description");

		// Assert.
		Assert.IsTrue(result.Contains("Pre-description"));
		Assert.IsTrue(result.Contains("Weekly Event Info"));
		Assert.IsTrue(result.Contains("Link:"));
		Assert.IsTrue(result.Contains("2025"));
	}

	[TestMethod]
	public async Task GetDescriptionForReplay_ShouldLogError_WhenWeeklyEventFails()
	{
		// Arrange.		
		_weeklyEventServiceMock!.GetStringForWeeklyEvent(_guildMock!, _battle!)
			.Returns<Task<string>>(x => throw new Exception("Failed to get weekly event string"));

		// Act.
		string result = await _replayService!.GetDescriptionForReplay(_guildMock!, _battle!, 0);

		// Assert.
		Assert.IsTrue(result.Contains("Link:"));
		_loggerMock!.Received().LogError(Arg.Any<Exception>(), "Error while getting weekly event description for replay.");
	}

	[TestMethod]
	public async Task GetReplayInfo_PlayerIdFound_WithAttachment_ReturnsBattle()
	{
		// Arrange.
		IDiscordAttachment attachment = Substitute.For<IDiscordAttachment>();
		attachment.Url.Returns("http://attachment.url");

		_discordAttachmentServiceMock!.DownloadAttachmentAsync(attachment)
			.Returns(Task.FromResult(("test.wotbreplay", new byte[] { 1, 2, 3 })));

		_battleRepositoryMock!.GetBattleAsync(Arg.Any<string>(), Arg.Any<byte[]>(), "title")
			.Returns(Task.FromResult<WotInspectorBattle?>(new WotInspectorBattle { Title = "title" }));

		// Act.
		WotInspectorBattle? result = await _replayService!.GetReplayInfo("title", attachment);

		// Assert.
		Assert.IsNotNull(result);
		Assert.AreEqual("title", result.Title);
	}

	[TestMethod]
	public async Task GetReplayInfo_NoAttachment_DoesNotReturnBattle()
	{
		// Arrange.
		_battleRepositoryMock!.GetBattleAsync(Arg.Any<string>(), Arg.Any<byte[]>(), "title")
			.Returns(Task.FromResult<WotInspectorBattle?>(null));

		// Act.
		WotInspectorBattle? result = await _replayService!.GetReplayInfo("title", null!);

		// Assert.
		Assert.IsNull(result);
	}

	[TestMethod]
	public async Task GetDescriptionForReplay_IncludesAchievements_WhenPresent()
	{
		// Arrange.
		Dictionary<string, WotbAchievement> achievements = new()
		{
			["sniperSeries"] = new WotbAchievement { AchievementId = "sniperSeries", Name = "Sniper", Description = "Hit from long distance", WotInspectorId = 403 }
		};

		_battle!.PlayersData[0].Achievements.Add("403", 1);
		_achievementsRepositoryMock!.GetAllAsync().Returns(Task.FromResult<Dictionary<string, WotbAchievement>?>(achievements));

		// Act.
		string result = await _replayService!.GetDescriptionForReplay(_guildMock!, _battle!, 0);

		// Assert.
		Assert.IsTrue(result.Contains("Sniper"));
	}
}
