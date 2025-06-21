namespace NLBE_Bot.Helpers;

using DSharpPlus.Entities;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

internal class GuildProvider(IDiscordClient discordClient) : IGuildProvider
{
	private readonly IDiscordClient _discordClient = discordClient;

	public IReadOnlyDictionary<ulong, IDiscordGuild> Guilds => _discordClient.Guilds;

	public async Task<IDiscordGuild> GetGuild(ulong serverID)
	{
		DiscordGuild guild = await _discordClient.GetGuildAsync(serverID);
		return new DiscordGuildWrapper(guild);
	}
}
