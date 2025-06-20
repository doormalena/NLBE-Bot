namespace NLBE_Bot.Interfaces;

using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

internal interface IUserService
{
	public Task<DiscordMember> GetDiscordMember(DiscordGuild guild, ulong userID);

	public Task ChangeMemberNickname(DiscordMember member, string nickname);

	public string UpdateName(DiscordMember member, string oldName);

	public Task UpdateUsers();

	public Tuple<string, string> GetIGNFromMember(string displayName);
}
