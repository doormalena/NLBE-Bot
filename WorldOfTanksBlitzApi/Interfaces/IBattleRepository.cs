namespace WorldOfTanksBlitzApi.Interfaces;

using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Models;

public interface IBattleRepository
{
	public Task<WotInspectorBattle?> GetBattleAsync(string fileName, byte[] fileContent, string title);
}
