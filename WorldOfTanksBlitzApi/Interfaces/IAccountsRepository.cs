namespace WorldOfTanksBlitzApi.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Models;

public interface IAccountsRepository
{
	public Task<IReadOnlyList<WotbAccountListItem>> SearchByNameAsync(SearchType searchType, string term, int maxResults = 20);

	public Task<WotbAccountInfo?> GetByIdAsync(long accountId);
}
