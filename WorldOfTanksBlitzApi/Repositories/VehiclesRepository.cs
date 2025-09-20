namespace WorldOfTanksBlitzApi.Interfaces;

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Models;


public class VehiclesRepository(IWotbConnection connection) : IVehiclesRepository
{
	private readonly IWotbConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));

	public async Task<WotbVehicle?> GetById(long tankId)
	{
		string relativeUrl = "encyclopedia/vehicles/";

		using MultipartFormDataContent form = [];
		form.Add(new StringContent(tankId.ToString()), "tank_id");

		string json = await _connection.PostAsync(relativeUrl, form);

		JsonElement root = JsonDocument.Parse(json).RootElement;

		return root.TryGetProperty("data", out JsonElement data)
			? JsonSerializer.Deserialize<WotbVehicle?>(data.GetRawText())
			: null;
	}
}
