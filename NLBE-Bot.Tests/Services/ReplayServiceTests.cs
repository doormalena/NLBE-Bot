namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

[TestClass]
public class ReplayServiceTests
{
	private ILogger<ReplayService>? _loggerMock;
	private IOptions<BotOptions>? _optionsMock;
	private IWeeklyEventService? _weeklyEventServiceMock;
	private IAccountsRepository? _accountRepositoryMock;
	private IBattleRepository? _battleRepositoryMock;
	private IAttachmentService? _discordAttachmentServiceMock;
	private WotbBattle? _battle;
	private IDiscordGuild? _guildMock;

	private ReplayService? _replayService;

	[TestInitialize]
	public void Setup()
	{
		_loggerMock = Substitute.For<ILogger<ReplayService>>();
		_optionsMock = Options.Create(new BotOptions());
		_weeklyEventServiceMock = Substitute.For<IWeeklyEventService>();
		_accountRepositoryMock = Substitute.For<IAccountsRepository>();
		_battleRepositoryMock = Substitute.For<IBattleRepository>();
		_discordAttachmentServiceMock = Substitute.For<IAttachmentService>();
		_guildMock = Substitute.For<IDiscordGuild>();

		_replayService = new ReplayService(
			_loggerMock,
			_optionsMock,
			_weeklyEventServiceMock,
			_accountRepositoryMock,
			_battleRepositoryMock,
			_discordAttachmentServiceMock
		);
		_battle = new();
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
		_accountRepositoryMock!.SearchByNameAsync(SearchType.Exact, "player name")
			.Returns(Task.FromResult<IReadOnlyList<WotbAccountListItem>>([new() { AccountId = 123 }]));

		IDiscordAttachment attachment = Substitute.For<IDiscordAttachment>();
		attachment.Url.Returns("http://attachment.url");

		_discordAttachmentServiceMock!.DownloadAttachmentAsync(attachment)
			.Returns(Task.FromResult(("test.wotbreplay", new byte[] { 1, 2, 3 })));

		_battleRepositoryMock!.GetBattle("test.wotbreplay", Arg.Any<byte[]>(), "title", 123)
			.Returns(Task.FromResult<WotbBattle?>(new WotbBattle { Summary = new WotbBattleSummary { Title = "title" } }));

		// Act.
		WotbBattle? result = await _replayService!.GetReplayInfo("title", attachment, "player name");

		// Assert.
		Assert.IsNotNull(result);
		Assert.AreEqual("title", result.Summary.Title);
	}

	[TestMethod]
	public async Task GetReplayInfo_PlayerIdFound_NoAttachment_UsesUrl()
	{

		// Arrange.
		_accountRepositoryMock!.SearchByNameAsync(SearchType.Exact, "IGN")
			.Returns(Task.FromResult<IReadOnlyList<WotbAccountListItem>>([new() { AccountId = 123 }]));

		_battleRepositoryMock!.GetBattle(Arg.Any<string>(), Arg.Any<byte[]>(), "title", 123)
			.Returns(Task.FromResult<WotbBattle?>(new WotbBattle { Summary = new WotbBattleSummary { Title = "title" } }));

		// Act.
		WotbBattle? result = await _replayService!.GetReplayInfo("title", null!, "IGN");

		// Assert.
		Assert.IsNotNull(result);
		Assert.AreEqual("title", result.Summary.Title);
	}

	[TestMethod]
	public async Task GetReplayInfo_PlayerIdNotFound_WithAttachment_ReturnsBattle()
	{

		// Arrange.
		_accountRepositoryMock!.SearchByNameAsync(SearchType.Exact, "player name")
			.Returns(Task.FromResult<IReadOnlyList<WotbAccountListItem>>([]));

		IDiscordAttachment attachment = Substitute.For<IDiscordAttachment>();
		attachment.Url.Returns("http://attachment.url");

		_discordAttachmentServiceMock!.DownloadAttachmentAsync(attachment)
			.Returns(Task.FromResult(("test.wotbreplay", new byte[] { 1, 2, 3 })));

		_battleRepositoryMock!.GetBattle("test.wotbreplay", Arg.Any<byte[]>(), "title", null)
			.Returns(Task.FromResult<WotbBattle?>(new WotbBattle { Summary = new WotbBattleSummary { Title = "title" } }));

		// Act.
		WotbBattle? result = await _replayService!.GetReplayInfo("title", attachment, "player name");

		// Assert.
		Assert.IsNotNull(result);
		Assert.AreEqual("title", result.Summary.Title);
	}

	[TestMethod]
	public async Task GetReplayInfo_PlayerIdNotFound_NoAttachment_ReturnsBattle()
	{
		// Arrange.
		_accountRepositoryMock!.SearchByNameAsync(SearchType.Exact, "IGN")
			.Returns(Task.FromResult<IReadOnlyList<WotbAccountListItem>>([]));

		_battleRepositoryMock!.GetBattle(Arg.Any<string>(), Arg.Any<byte[]>(), "title", null)
			.Returns(Task.FromResult<WotbBattle?>(new WotbBattle { Summary = new WotbBattleSummary { Title = "title" } }));

		// Act.
		WotbBattle? result = await _replayService!.GetReplayInfo("title", null!, "IGN");

		// Assert.
		Assert.IsNotNull(result);
		Assert.AreEqual("title", result.Summary.Title);
	}
}
