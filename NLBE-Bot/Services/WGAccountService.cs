namespace NLBE_Bot.Services;

using FMWOTB.Account;
using FMWOTB.Tools;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

internal class WGAccountService : IWGAccountService
{
	public async Task<IReadOnlyList<IWGAccount>> SearchByName(SearchAccuracy accuracy, string term, string applicationKey, bool loadClanMembers, bool loadClan, bool loadStatistics)
	{
		return await WGAccount.searchByName(accuracy, term, applicationKey, loadClanMembers, loadClan, loadStatistics)
					.ContinueWith(t => (IReadOnlyList<IWGAccount>) [.. t.Result.Select(u => new WGAccountWrapper(u))]);
	}
}
