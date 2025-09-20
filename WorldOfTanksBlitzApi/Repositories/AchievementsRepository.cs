namespace WorldOfTanksBlitzApi.Repositories;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

public class AchievementsRepository : IAchievementsRepository
{
	private readonly IWotbConnection _connection;
	private readonly IWotInspectorAchievementMappingProvider _mappingProvider;

	public AchievementsRepository(IWotbConnection connection, IWotInspectorAchievementMappingProvider mappingProvider)
	{
		_connection = connection ?? throw new ArgumentNullException(nameof(connection));
		_mappingProvider = mappingProvider ?? throw new ArgumentNullException(nameof(mappingProvider));
	}

	public async Task<Dictionary<string, WotbAchievement>?> GetAllAsync()
	{
		string relativeUrl = "/encyclopedia/achievements/";

		using MultipartFormDataContent form = [];
		string json = await _connection.PostAsync(relativeUrl, form);

		JsonNode? rootNode = JsonNode.Parse(json);
		JsonNode? dataNode = rootNode?["data"];

		Dictionary<string, WotbAchievement>? achievements = dataNode != null && dataNode.ToJsonString() != "null"
			? JsonSerializer.Deserialize<Dictionary<string, WotbAchievement>>(dataNode.ToJsonString())
			: null;

		await PopulateWotInspectorId(achievements);

		return achievements;
	}

	private async Task PopulateWotInspectorId(Dictionary<string, WotbAchievement>? achievements)
	{
		Dictionary<string, string>? mapping = await _mappingProvider.GetMappingAsync();
		if (achievements != null && mapping != null)
		{
			foreach (KeyValuePair<string, string> mapEntry in mapping)
			{
				if (int.TryParse(mapEntry.Key, out int wotInspectorId) &&
					achievements.TryGetValue(mapEntry.Value, out WotbAchievement? achievement))
				{
					achievement.WotInspectorId = wotInspectorId;
				}
			}
		}
	}
}
