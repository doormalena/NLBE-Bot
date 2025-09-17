namespace WorldOfTanksBlitzApi.Interfaces;

using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Models;

public interface IBattleRepository
{
	public Task<WotbBattle?> GetBattle(string fileName, byte[] fileContent, string? title, long? accountId);
}
