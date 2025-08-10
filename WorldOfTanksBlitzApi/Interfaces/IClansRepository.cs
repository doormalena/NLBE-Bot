namespace FMWOTB.Interfaces;

using FMWOTB;
using FMWOTB.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IClansRepository
{
	public Task<IReadOnlyList<WotbClanListItem>> SearchByNameAsync(SearchType searchType, string term, bool loadMembers = false, int maxResults = 20);

	public Task<WotbClanInfo> GetByIdAsync(long clanId, bool loadMembers = false);
}
