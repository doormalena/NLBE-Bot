namespace WorldOfTanksBlitzApi.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Models;

public interface IMapsRepository
{
	public Task<Dictionary<string, MapInfo>?> GetAllAsync();
}
