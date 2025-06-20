namespace NLBE_Bot.Interfaces;

using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using FMWOTB.Account;
using FMWOTB.Clans;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal interface IBot
{
	public Task RunAsync();

	public bool CheckIfAllWithinRange(string[] tiers, int min, int max);

	public Task ShowMemberInfo(DiscordChannel channel, object gebruiker);

	public Task ShowClanInfo(DiscordChannel channel, WGClan clan);

	public Task<WGClan> SearchForClan(DiscordChannel channel, DiscordMember member, string guildName, string clan_naam, bool loadMembers, DiscordUser user, Command command);

	public Task<WGAccount> SearchPlayer(DiscordChannel channel, DiscordMember member, DiscordUser user, string guildName, string naam);

	public bool HasRight(DiscordMember member, Command command);

	public List<string> GetSearchTermAndCondition(params string[] parameter);

	public Task<List<Tuple<ulong, string>>> GetIndividualParticipants(List<Tier> teams, DiscordGuild guild);

	public Task<List<string>> GetMentions(List<Tuple<ulong, string>> memberList, ulong guildID);

	public List<DEF> ListInPlayerEmbed(int columns, List<Members> memberList, string searchTerm, DiscordGuild guild);

	public List<DEF> ListInMemberEmbed(int columns, List<DiscordMember> memberList, string searchTerm);

	public List<Tuple<ulong, string>> RemoveSyntaxes(List<Tuple<ulong, string>> stringList);

	public Task<List<Tier>> ReadTeams(DiscordChannel channel, DiscordMember member, string guildName, string[] parameters_as_in_hoeveelste_team);
}
