namespace NLBE_Bot.Helpers;

using DSharpPlus;
using DSharpPlus.Entities;
using NLBE_Bot.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

internal class GuildProvider(IDiscordClientWrapper discordClient) : IGuildProvider
{
	private readonly IDiscordClientWrapper _discordClient = discordClient;

	public IReadOnlyDictionary<ulong, DiscordGuild> Guilds => _discordClient.Guilds;

	public Task<DiscordGuild> GetGuild(ulong serverID)
	{
		return _discordClient.GetGuildAsync(serverID);
	}
}
