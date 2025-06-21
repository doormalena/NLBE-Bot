namespace NLBE_Bot.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IMapService
{
	public Task<List<Tuple<string, string>>> GetAllMaps(ulong guildId);
}
