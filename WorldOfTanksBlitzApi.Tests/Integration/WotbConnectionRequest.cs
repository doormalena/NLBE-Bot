namespace WorldOfTanksBlitzApi.Tests.Integration;

using Microsoft.Extensions.Logging;
using NSubstitute;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;
using WorldOfTanksBlitzApi.Repositories;

[TestClass, Ignore("Only use locally to test integration with real API")]
public class WotbConnectionRequestTests
{
	private HttpClient? _httpClient;
	private ILogger<WotbConnection>? _loggerMock;
	private WotbConnection? _connection;
	private WotInspectorConnection? _inspectorConnection;
	private AchievementsRepository? _achievementsRepository;
	private BattleRepository? _battleRepository;
	private readonly string BaseUri = "https://api.wotblitz.eu/wotb";
	private readonly string BaseUriInspector = "https://api.wotinspector.com"; // "https://wotinspector.com/api"
	private readonly string ApplicationId = "3b60bca822c6f4f0e5effbc0ff7db172"; // Replace with your actual application ID

	[TestInitialize]
	public void Setup()
	{
		_httpClient = new HttpClient();
		_loggerMock = Substitute.For<ILogger<WotbConnection>>();
		_connection = new(_httpClient, _loggerMock, BaseUri, ApplicationId);
		_inspectorConnection = new(_httpClient, Substitute.For<ILogger<WotInspectorConnection>>(), BaseUriInspector);
		IWotInspectorAchievementMappingProvider mappingProviderMock = Substitute.For<IWotInspectorAchievementMappingProvider>();
		_achievementsRepository = new AchievementsRepository(_connection, mappingProviderMock);
		_battleRepository = new BattleRepository(_inspectorConnection);
	}

	[TestMethod]
	public async Task AchievementsRepository_GetAllAsync_ReturnsAchievements()
	{
		// Arrange
		string fileName = "20250909_1926__Kqb658kbgy_Ch28_WZ_132A_580965027998101816.wotbreplay";
		byte[] fileContent = await File.ReadAllBytesAsync(Path.Combine("C:\\Users\\alexander\\OneDrive\\WoT\\Ace tankers\\", fileName));

		// Act
		WotInspectorBattle? battle = await _battleRepository!.GetBattle(fileName, fileContent, "test");

		Dictionary<string, WotbAchievement>? _ = await _achievementsRepository!.GetAllAsync();

		// Assert
		Assert.IsNotNull(battle);
	}
}
