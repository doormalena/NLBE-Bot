namespace WorldOfTanksBlitzApi.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Models;

public interface IVehiclesRepository
{
	public Task<WotbVehicle?> GetById(long tankId);
}

