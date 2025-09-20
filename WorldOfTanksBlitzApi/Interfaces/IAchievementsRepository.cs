namespace WorldOfTanksBlitzApi.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Models;

public interface IAchievementsRepository
{
	public Task<Dictionary<string, WotbAchievement>?> GetAllAsync();
}
