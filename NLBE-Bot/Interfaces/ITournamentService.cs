namespace NLBE_Bot.Interfaces;

using DSharpPlus.Entities;
using FMWOTB.Tournament;
using System.Collections.Generic;
using System.Threading.Tasks;

internal interface ITournamentService
{
	public Task GenerateLogMessage(DiscordMessage message, DiscordChannel toernooiAanmeldenChannel, ulong userID, string emojiAsEmoji);

	public Task<List<WGTournament>> InitialiseTournaments(bool all);

	public Task ShowTournamentInfo(DiscordChannel channel, WGTournament tournament, string titel);
}
