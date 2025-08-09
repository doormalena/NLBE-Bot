namespace FMWOTB.Interfaces;

using FMWOTB.Models;
using FMWOTB.Tools;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IAccountsRepository
{
	public Task<IReadOnlyList<PlayerInfo>> SearchByNameAsync(SearchAccuracy accuracy, string term, bool loadClanMembers = false, bool loadClan = false, bool loadStatistics = false, short loadVehicles = 0, int maxResults = 20);

	public Task<PlayerInfo> GetByIdAsync(long accountId, bool loadClanMembers = false, bool loadClan = false, bool loadStatistics = false, short loadVehicles = 0);
}
