namespace WorldOfTanksBlitzApi.Tools;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Interfaces;

public class WotInspectorAchievementMappingProvider(string resourceName = "WorldOfTanksBlitzApi.Resources.WotInspectorAchievementMapping.json") : IWotInspectorAchievementMappingProvider
{
	private readonly string _resourceName = resourceName ?? throw new ArgumentNullException(nameof(resourceName));

	public async Task<Dictionary<string, string>?> GetMappingAsync()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		using Stream? stream = assembly.GetManifestResourceStream(_resourceName) ?? throw new FileNotFoundException($"Embedded resource '{_resourceName}' not found.");
		using StreamReader reader = new(stream);
		string json = await reader.ReadToEndAsync();
		return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
	}
}
