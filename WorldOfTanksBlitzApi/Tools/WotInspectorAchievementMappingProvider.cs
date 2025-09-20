namespace WorldOfTanksBlitzApi.Tools;

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Interfaces;

public class WotInspectorAchievementMappingProvider : IWotInspectorAchievementMappingProvider
{
	private const string ResourceName = "WorldOfTanksBlitzApi.Tools.WotInspectorAchievementMapping.json";

	public async Task<Dictionary<string, string>?> GetMappingAsync()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();

		using Stream? stream = assembly.GetManifestResourceStream(ResourceName) ?? throw new FileNotFoundException($"Embedded resource '{ResourceName}' not found.");

		using StreamReader reader = new(stream);
		string json = await reader.ReadToEndAsync();

		return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
	}
}
