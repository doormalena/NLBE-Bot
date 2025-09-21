namespace WorldOfTanksBlitzApi.Interfaces;

using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Models;

public interface IVehiclesRepository
{
	public Task<WotbVehicle?> GetByIdAsync(long tankId);
}

