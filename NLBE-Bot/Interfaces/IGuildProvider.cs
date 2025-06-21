namespace NLBE_Bot.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;

internal interface IGuildProvider
{
	public Task<IDiscordGuild> GetGuild(ulong serverID);

	public IReadOnlyDictionary<ulong, IDiscordGuild> Guilds
	{
		get;
	}
}
