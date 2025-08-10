namespace FMWOTB.Interfaces;

using FMWOTB;
using FMWOTB.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IAccountsRepository
{
	public Task<IReadOnlyList<WotbAccountListItem>> SearchByNameAsync(SearchType searchType, string term, int maxResults = 20);

	public Task<WotbAccountInfo> GetByIdAsync(long accountId);
}
