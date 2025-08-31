namespace WorldOfTanksBlitzApi.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Models;

public interface IClansRepository
{
	public Task<IReadOnlyList<WotbClanListItem>> SearchByNameAsync(SearchType searchType, string term, bool loadMembers = false, int maxResults = 20);

	public Task<WotbClanInfo> GetByIdAsync(long clanId, bool loadMembers = false);

	public Task<WotbAccountClanInfo> GetAccountClanInfoAsync(long accountId);
}
