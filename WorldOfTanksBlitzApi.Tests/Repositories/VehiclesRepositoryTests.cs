namespace WorldOfTanksBlitzApi.Tests.Repositories;

using NSubstitute;
using System.Net.Http;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;
using WorldOfTanksBlitzApi.Repositories;

[TestClass]
public class VehiclesRepositoryTests
{
	private IWotbConnection? _connectionMock;
	private VehiclesRepository? _repository;

	[TestInitialize]
	public void Setup()
	{
		_connectionMock = Substitute.For<IWotbConnection>();
		_repository = new VehiclesRepository(_connectionMock);
	}

	#region GetByIdAsync Tests

	[TestMethod]
	public async Task GetByIdAsync_ReturnsVehicle_WhenApiReturnsData()
	{
		// Arrange
		long tankId = 123;
		string json = $$"""
		{
			"data": {
				"{{tankId}}": {
					"tank_id": {{tankId}},
					"name": "Tank A",
					"description": "Desc A",
					"tier": 7,
					"type": "medium",
					"nation": "usa",
					"is_premium": false,
					"engines": [1,2],
					"guns": [3,4],
					"suspensions": [5],
					"turrets": [6]
				}
			}
		}
		""";

		_connectionMock!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>())
			.Returns(Task.FromResult(json));

		// Act
		WotbVehicle? vehicle = await _repository!.GetByIdAsync(tankId);

		// Assert
		Assert.IsNotNull(vehicle);
		Assert.AreEqual(tankId, vehicle.TankId);
		Assert.AreEqual("Tank A", vehicle.Name);
		Assert.AreEqual(7, vehicle.Tier);
		Assert.AreEqual("medium", vehicle.Type);
		Assert.AreEqual("usa", vehicle.Nation);
		Assert.IsFalse(vehicle.IsPremium);
		Assert.AreEqual(2, vehicle.Engines.Count);
		Assert.AreEqual(2, vehicle.Guns.Count);
		Assert.AreEqual(1, vehicle.Suspensions.Count);
		Assert.AreEqual(1, vehicle.Turrets.Count);
	}

	[TestMethod]
	[DataRow("{\"data\": null}")]
	[DataRow("{\"data\":{}}")]
	public async Task GetByIdAsync_ReturnsNull_WhenDataIsMissing(string json)
	{
		// Arrange
		long tankId = 123;
		_connectionMock!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>())
			.Returns(Task.FromResult(json));

		// Act
		WotbVehicle? vehicle = await _repository!.GetByIdAsync(tankId);

		// Assert
		Assert.IsNull(vehicle);
	}

	#endregion

	#region GetAllAsync Tests

	[TestMethod]
	public async Task GetAllAsync_ReturnsVehicles_WhenApiReturnsData()
	{
		// Arrange.
		string vehiclesJson = """
    {
        "101": {
            "tank_id": 101,
            "name": "Tank A",
            "description": "Desc A",
            "tier": 5,
            "type": "medium",
            "nation": "usa",
            "is_premium": false,
            "engines": [1],
            "guns": [2],
            "suspensions": [3],
            "turrets": [4]
        },
        "202": {
            "tank_id": 202,
            "name": "Tank B",
            "description": "Desc B",
            "tier": 8,
            "type": "heavy",
            "nation": "germany",
            "is_premium": true,
            "engines": [10, 11],
            "guns": [20],
            "suspensions": [30],
            "turrets": [40, 41]
        }
    }
    """;

		string apiResponse = $"{{\"data\": {vehiclesJson} }}";

		_connectionMock!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>())
			.Returns(Task.FromResult(apiResponse));

		// Act.
		Dictionary<string, WotbVehicle>? vehicles = await _repository!.GetAllAsync();

		// Assert.
		Assert.IsNotNull(vehicles);
		Assert.AreEqual(2, vehicles.Count);

		Assert.IsTrue(vehicles.ContainsKey("101"));
		Assert.AreEqual(101, vehicles["101"].TankId);
		Assert.AreEqual("Tank A", vehicles["101"].Name);
		Assert.AreEqual(5, vehicles["101"].Tier);
		Assert.AreEqual("medium", vehicles["101"].Type);
		Assert.AreEqual("usa", vehicles["101"].Nation);
		Assert.IsFalse(vehicles["101"].IsPremium);
		Assert.AreEqual(1, vehicles["101"].Engines.Count);
		Assert.AreEqual(1, vehicles["101"].Guns.Count);
		Assert.AreEqual(1, vehicles["101"].Suspensions.Count);
		Assert.AreEqual(1, vehicles["101"].Turrets.Count);

		Assert.IsTrue(vehicles.ContainsKey("202"));
		Assert.AreEqual(202, vehicles["202"].TankId);
		Assert.AreEqual("Tank B", vehicles["202"].Name);
		Assert.AreEqual(8, vehicles["202"].Tier);
		Assert.AreEqual("heavy", vehicles["202"].Type);
		Assert.AreEqual("germany", vehicles["202"].Nation);
		Assert.IsTrue(vehicles["202"].IsPremium);
		Assert.AreEqual(2, vehicles["202"].Engines.Count);
		Assert.AreEqual(1, vehicles["202"].Guns.Count);
		Assert.AreEqual(1, vehicles["202"].Suspensions.Count);
		Assert.AreEqual(2, vehicles["202"].Turrets.Count);
	}

	[TestMethod]
	public async Task GetAllAsync_ReturnsNull_WhenApiReturnsNullData()
	{
		// Arrange.
		string apiResponse = "{\"data\": null}";

		_connectionMock!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>())
			.Returns(Task.FromResult(apiResponse));

		// Act.
		Dictionary<string, WotbVehicle>? vehicles = await _repository!.GetAllAsync();

		// Assert.
		Assert.IsNull(vehicles);
	}

	[TestMethod]
	public async Task GetAllAsync_ReturnsNull_WhenDataNodeMissing()
	{
		// Arrange.
		string apiResponse = """
    {
        "meta": {},
        "status": "ok"
    }
    """;

		_connectionMock!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>())
			.Returns(Task.FromResult(apiResponse));

		// Act.
		Dictionary<string, WotbVehicle>? vehicles = await _repository!.GetAllAsync();

		// Assert.
		Assert.IsNull(vehicles);
	}

	[TestMethod]
	public async Task GetAllAsync_ReturnsEmptyDictionary_WhenDataIsEmptyObject()
	{
		// Arrange.
		string apiResponse = "{\"data\": {}}";

		_connectionMock!.PostAsync(Arg.Any<string>(), Arg.Any<MultipartFormDataContent>())
			.Returns(Task.FromResult(apiResponse));

		// Act.
		Dictionary<string, WotbVehicle>? vehicles = await _repository!.GetAllAsync();

		// Assert.
		Assert.IsNotNull(vehicles);
		Assert.AreEqual(0, vehicles.Count);
	}

	#endregion
}
