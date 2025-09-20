namespace WorldOfTanksBlitzApi.Repositories;

using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

public class BattleRepository(IWotInspectorConnection connection) : IBattleRepository
{
	private readonly IWotInspectorConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));

	public async Task<WotInspectorBattle?> GetBattle(string fileName, byte[] fileContent, string title)
	{
		const string relativeUrl = "/v2/blitz/replays/";

		string json = await _connection.UploadReplayAsync(relativeUrl, fileName, fileContent, title);

		// The API returns: { "id": "833df4545fg45246b4e31038f539f5", "map_id": 23, ... }
		JsonNode? rootNode = JsonNode.Parse(json);

		return rootNode != null ?
			JsonSerializer.Deserialize<WotInspectorBattle>(rootNode.ToJsonString()) :
			null;
	}
}
