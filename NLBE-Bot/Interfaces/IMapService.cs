namespace NLBE_Bot.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Models;

internal interface IMapService
{
	public Task<Dictionary<string, MapInfo>> GetAllMaps(IDiscordGuild guild);
}
