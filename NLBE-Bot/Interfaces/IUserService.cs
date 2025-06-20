namespace NLBE_Bot.Interfaces;

using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using FMWOTB.Account;
using FMWOTB.Clans;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal interface IUserService
{
	public Task<DiscordMember> GetDiscordMember(DiscordGuild guild, ulong userID);

	public Task ChangeMemberNickname(DiscordMember member, string nickname);

	public string UpdateName(DiscordMember member, string oldName);

	public Task UpdateUsers();

	public Tuple<string, string> GetIGNFromMember(string displayName);

	public Task ShowMemberInfo(DiscordChannel channel, object gebruiker);

	public List<DEF> ListInPlayerEmbed(int columns, List<Members> memberList, string searchTerm, DiscordGuild guild);

	public List<DEF> ListInMemberEmbed(int columns, List<DiscordMember> memberList, string searchTerm);

	public Task<WGAccount> SearchPlayer(DiscordChannel channel, DiscordMember member, DiscordUser user, string guildName, string naam);

	public bool HasPermission(DiscordMember member, Command command);
}
