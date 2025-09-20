namespace WorldOfTanksBlitzApi.Repositories;

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

public class MapsRepository() : IMapsRepository
{
	public async Task<Dictionary<string, MapInfo>?> GetAllAsync()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		const string resourceName = "WorldOfTanksBlitzApi.Resources.Maps.json"; // Source: https://github.com/Jylpah/blitz-replays/blob/main/src/blitzreplays/maps.json

		using Stream? stream = assembly.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");

		using StreamReader reader = new(stream);
		string json = await reader.ReadToEndAsync();

		return JsonSerializer.Deserialize<Dictionary<string, MapInfo>>(json);
	}
}
