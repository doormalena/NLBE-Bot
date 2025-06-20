namespace NLBE_Bot.Interfaces;

using DSharpPlus.Entities;
using FMWOTB.Tournament;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal interface ITournamentService
{
	public Task GenerateLogMessage(DiscordMessage message, DiscordChannel toernooiAanmeldenChannel, ulong userID, string emojiAsEmoji);

	public Task<List<WGTournament>> InitialiseTournaments(bool all);

	public Task ShowTournamentInfo(DiscordChannel channel, WGTournament tournament, string titel);

	public Task<List<Tier>> ReadTeams(DiscordChannel channel, DiscordMember member, string guildName, string[] parameters_as_in_hoeveelste_team);

	public Task<List<Tuple<ulong, string>>> GetIndividualParticipants(List<Tier> teams, DiscordGuild guild);

	public bool CheckIfAllWithinRange(string[] tiers, int min, int max);

	public Task<List<string>> GetMentions(List<Tuple<ulong, string>> memberList, ulong guildID);
}
