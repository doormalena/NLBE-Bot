namespace WorldOfTanksBlitzApi.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;

public interface IWotInspectorAchievementMappingProvider
{
	public Task<Dictionary<string, string>?> GetMappingAsync();
}
