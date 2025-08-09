namespace NLBE_Bot.Interfaces;

using FMWOTB.Tools;
using System.Collections.Generic;
using System.Threading.Tasks;

internal interface IWGAccountService
{
	public Task<IReadOnlyList<IWGAccount>> SearchByName(SearchAccuracy accuracy, string term, string applicationKey, bool loadClanMembers, bool loadClan, bool loadStatistics);
}
