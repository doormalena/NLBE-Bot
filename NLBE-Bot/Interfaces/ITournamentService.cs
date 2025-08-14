namespace NLBE_Bot.Interfaces;

using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Tournament;

internal interface ITournamentService
{
	public Task GenerateLogMessage(IDiscordMessage message, IDiscordChannel toernooiAanmeldenChannel, ulong userID, string emojiAsEmoji);

	public Task<List<WGTournament>> InitialiseTournaments(bool all);

	public Task ShowTournamentInfo(IDiscordChannel channel, WGTournament tournament, string titel);

	public Task<List<Tier>> ReadTeams(IDiscordChannel channel, IDiscordMember member, string guildName, string[] parameters_as_in_hoeveelste_team);

	public Task<List<Tuple<ulong, string>>> GetIndividualParticipants(List<Tier> teams, IDiscordGuild guild);

	public bool CheckIfAllWithinRange(string[] tiers, int min, int max);

	public Task<List<string>> GetMentions(List<Tuple<ulong, string>> memberList, ulong guildID);
}
