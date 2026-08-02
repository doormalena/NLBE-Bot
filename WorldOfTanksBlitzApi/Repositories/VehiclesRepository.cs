namespace WorldOfTanksBlitzApi.Repositories;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

public class VehiclesRepository(IWotbConnection connection) : IVehiclesRepository
{
	private readonly IWotbConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));

	public async Task<Dictionary<string, WotbVehicle>?> GetAllAsync()
	{
		string relativeUrl = "/encyclopedia/vehicles/";

		using MultipartFormDataContent form = [];
		string json = await _connection.PostAsync(relativeUrl, form);

		JsonNode? rootNode = JsonNode.Parse(json);
		JsonNode? dataNode = rootNode?["data"];

		Dictionary<string, WotbVehicle>? vehicles = dataNode != null && dataNode.ToJsonString() != "null"
			? JsonSerializer.Deserialize<Dictionary<string, WotbVehicle>>(dataNode.ToJsonString())
			: null;

		return vehicles;
	}

	public async Task<WotbVehicle?> GetByIdAsync(long tankId)
	{
		string relativeUrl = "encyclopedia/vehicles/";

		using MultipartFormDataContent form = [];
		form.Add(new StringContent(tankId.ToString()), "tank_id");

		string json = await _connection.PostAsync(relativeUrl, form);

		JsonNode? rootNode = JsonNode.Parse(json);
		JsonNode? dataNode = rootNode?["data"];

		if (dataNode != null)
		{
			JsonNode? vehicleNode = dataNode[tankId.ToString()];

			if (vehicleNode != null && vehicleNode.ToJsonString() != "null")
			{
				return JsonSerializer.Deserialize<WotbVehicle>(vehicleNode.ToJsonString());
			}
		}

		return null;
	}
}
