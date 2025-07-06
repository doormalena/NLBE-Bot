namespace NLBE_Bot.Interfaces;

using FMWOTB.Account;
using FMWOTB.Clans;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal interface IUserService
{
	public Task<IDiscordMember> GetDiscordMember(IDiscordGuild guild, ulong userID);

	public Task ChangeMemberNickname(IDiscordMember member, string nickname);

	public string UpdateName(IDiscordMember member, string oldName);

	public Tuple<string, string> GetWotbPlayerNameFromDisplayName(string displayName);

	public Task ShowMemberInfo(IDiscordChannel channel, object gebruiker);

	public Task<List<DEF>> ListInPlayerEmbed(int columns, List<Members> memberList, string searchTerm, IDiscordGuild guild);

	public List<DEF> ListInMemberEmbed(int columns, List<IDiscordMember> memberList, string searchTerm);

	public Task<WGAccount> SearchPlayer(IDiscordChannel channel, IDiscordMember member, IDiscordUser user, string guildName, string naam);

	public bool HasPermission(IDiscordMember member, IDiscordCommand command);
}
