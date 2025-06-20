namespace NLBE_Bot.Interfaces;

using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

internal interface IGuildProvider
{
	public Task<DiscordGuild> GetGuild(ulong serverID);

	public IReadOnlyDictionary<ulong, DiscordGuild> Guilds
	{
		get;
	}
}
