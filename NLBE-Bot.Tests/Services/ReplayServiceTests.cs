namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Tools.Replays;

[TestClass]
public class ReplayServiceTests
{
	private ILogger<ReplayService>? _loggerMock;
	private IOptions<BotOptions>? _optionsMock;
	private IWeeklyEventService? _weeklyEventServiceMock;
	private IAccountsRepository? _accountRepositoryMock;
	private WGBattle? _battle;
	private IDiscordGuild? _guildMock;

	private ReplayService? _replayService;

	[TestInitialize]
	public void Setup()
	{
		_loggerMock = Substitute.For<ILogger<ReplayService>>();
		_optionsMock = Options.Create(new BotOptions());
		_weeklyEventServiceMock = Substitute.For<IWeeklyEventService>();
		_accountRepositoryMock = Substitute.For<IAccountsRepository>();
		_guildMock = Substitute.For<IDiscordGuild>();

		_replayService = new ReplayService(
			_loggerMock,
			_optionsMock,
			_weeklyEventServiceMock,
			_accountRepositoryMock
		);

		string battleJson = """
		{
		  "status": "ok",
		  "data": {
		    "view_url": "https://replays.wotinspector.com/en/view/1c21a9873cdab07aa24a1a9c4804dfcb",
		    "summary": {
		      "title": "VK 72.01 (K), train",
		      "player_name": "Kqb658kbgy",
		      "map_name": "train",
		      "vehicle": "VK 72.01 (K)",
		      "vehicle_tier": 10,
		      "protagonist_team": 1,
		      "winner_team": 1,
		      "battle_type": 1,
		      "room_type": 1,
		      "battle_start_time": "2025-09-01 19:59:13",
		      "details": {
		        "clan_tag": "NLBE",
		        "damage_made": 4548,
		        "damage_blocked": 1440,
		        "damage_assisted": 632,
		        "damage_assisted_track": 0,
		        "exp": 1496,
		        "shots_pen": 9,
		        "enemies_destroyed": 2,
		        "achievements": [
		          { "t": "411", "v": 1 },
		          { "t": "407", "v": 2 },
		          { "t": "403", "v": 1 },
		          { "t": "448", "v": 2 },
		          { "t": "409", "v": 1 }
		        ]
		      }
		    }
		  }
		}
		""";
		_battle = new(battleJson);
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
	}

	[TestMethod]
	public async Task GetDescriptionForReplay_ShouldLogError_WhenWeeklyEventFails()
	{
		// Arrange.
		_weeklyEventServiceMock!.GetStringForWeeklyEvent(_guildMock!, _battle!).Throws(new Exception("Boom"));

		// Act.
		string result = await _replayService!.GetDescriptionForReplay(_guildMock!, _battle!, 0);

		// Assert.
		Assert.IsTrue(result.Contains("Link:"));
		_loggerMock!.Received().LogError(Arg.Any<Exception>(), "Error while getting weekly event description for replay.");
	}
}
