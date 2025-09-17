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

	public async Task<WotbBattle?> GetBattle(string fileName, byte[] fileContent, string? title, long? accountId)
	{
		const string relativeUrl = "/replay/upload?url=";

		string json = await _connection.UploadReplayAsync(relativeUrl, fileName, fileContent, title, accountId);

		// The API returns: { "status": "...", "data": { "summary": { ...details... } } }
		JsonNode? rootNode = JsonNode.Parse(json);
		JsonNode? dataNode = rootNode?["data"];

		return dataNode != null && dataNode.ToJsonString() != "null" ?
			JsonSerializer.Deserialize<WotbBattle>(dataNode.ToJsonString()) :
			null;
	}
}
