namespace WorldOfTanksBlitzApi.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Models;

public interface IVehiclesRepository
{
	public Task<Dictionary<string, WotbVehicle>?> GetAllAsync();

	public Task<WotbVehicle?> GetByIdAsync(long tankId);
}

