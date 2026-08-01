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
}
