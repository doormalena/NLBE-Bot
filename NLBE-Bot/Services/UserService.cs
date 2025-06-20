namespace NLBE_Bot.Services;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Net.Models;
using FMWOTB.Account;
using FMWOTB.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class UserService(DiscordClient discordClient, IErrorHandler errorHandler, IConfiguration configuration, IChannelService channelService, IMessageService messageService) : IUserService
{
	private readonly DiscordClient _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

	public async Task<DiscordMember> GetDiscordMember(DiscordGuild guild, ulong userID)
	{
		return await guild.GetMemberAsync(userID);
	}

	public async Task ChangeMemberNickname(DiscordMember member, string nickname)
	{
		try
		{
			void mem(MemberEditModel item)
			{
				item.Nickname = nickname;
				item.AuditLogReason = "Changed by NLBE-Bot";
			}
			await member.ModifyAsync(mem);
		}
		catch (Exception ex)
		{
			await _errorHandler.HandleErrorAsync("Could not edit displayname for " + member.Username + ":", ex);
		}
	}

	public string UpdateName(DiscordMember member, string oldName)
	{
		string returnString = oldName;
		IEnumerable<DiscordRole> memberRoles = member.Roles;
		if (oldName.Contains('[') && oldName.Contains(']'))
		{
			string[] splitted = oldName.Split('[');
			StringBuilder sb = new();
			if (oldName.StartsWith('['))
			{
				for (int i = 1; i < splitted.Length; i++)
				{
					sb.Append(splitted[i]);
				}
				splitted = sb.ToString().Split(']');

				sb.Clear();

				foreach (DiscordRole role in memberRoles)
				{
					if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE))
					{
						if (!oldName.StartsWith("[" + role.Name + "]"))
						{
							returnString = oldName.Replace("[" + splitted[0] + "]", "[" + role.Name + "]");
						}
						if (!returnString.StartsWith("[" + role.Name + "] "))
						{
							returnString = returnString.Replace("[" + role.Name + "]", "[" + role.Name + "] ");
						}
						break;
					}
				}
			}
			else
			{
				foreach (DiscordRole role in memberRoles)
				{
					if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE))
					{
						if (oldName.StartsWith(role.Name + "] "))
						{
							returnString = "[" + oldName;
						}
						else
						{
							for (int i = 1; i < splitted.Length; i++)
							{
								if (i > 1)
								{
									sb.Append('[');
								}
								sb.Append(splitted[i]);
							}
							splitted = sb.ToString().Split(']');
							sb = new StringBuilder();
							for (int i = 1; i < splitted.Length; i++)
							{
								if (i > 1)
								{
									sb.Append(']');
								}
								sb.Append(splitted[i]);
							}
							returnString = "[" + role.Name + "] " + sb.ToString();
						}
					}
				}
			}

		}
		else if (oldName.Contains('['))
		{
			foreach (DiscordRole role in memberRoles)
			{
				if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE))
				{
					if (!oldName.StartsWith("[" + role.Name))
					{
						string[] splitted = oldName.Split(' ');
						if (splitted.Length > 1)
						{
							StringBuilder sb = new();
							for (int i = 1; i < splitted.Length; i++)
							{
								if (i > 1)
								{
									sb.Append(' ');
								}
								sb.Append(splitted[i]);
							}
							returnString = "[" + role.Name + "] " + sb.ToString();
						}
						else
						{
							returnString = "[" + role.Name + "] " + oldName.Replace("[", string.Empty);
						}
					}
					else
					{
						string[] splitted = oldName.Split(' ');
						if (splitted.Length > 1)
						{
							StringBuilder sb = new();
							for (int i = 1; i < splitted.Length; i++)
							{
								if (i > 1)
								{
									sb.Append(' ');
								}
								sb.Append(splitted[i]);
							}
							returnString = "[" + role.Name + "] " + sb.ToString();
						}
						else
						{
							returnString = oldName.Replace("[" + role.Name, "[" + role.Name + "] ") + oldName.Replace("[" + role.Name, string.Empty);
						}
					}
				}
			}
		}
		else if (oldName.Contains(']'))
		{
			foreach (DiscordRole role in memberRoles)
			{
				if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE))
				{
					if (!oldName.StartsWith(role.Name + "]"))
					{
						string[] splitted = oldName.Split(' ');
						if (splitted.Length > 1)
						{
							StringBuilder sb = new();
							for (int i = 1; i < splitted.Length; i++)
							{
								if (i > 1)
								{
									sb.Append(' ');
								}
								sb.Append(splitted[i]);
							}
							returnString = "[" + role.Name + "] " + sb.ToString().Replace("]", string.Empty);
						}
						else
						{
							returnString = "[" + role.Name + "] " + oldName.Replace("]", string.Empty);
						}
					}
					else
					{
						string[] splitted = oldName.Split(' ');
						if (splitted.Length > 1)
						{
							StringBuilder sb = new();
							for (int i = 1; i < splitted.Length; i++)
							{
								if (i > 1)
								{
									sb.Append(' ');
								}
								sb.Append(splitted[i]);
							}
							returnString = "[" + role.Name + "] " + sb.ToString();
						}
						else
						{
							returnString = oldName.Replace(role.Name + "]", "[" + role.Name + "] ") + oldName.Replace("]" + role.Name, string.Empty);
						}
					}
				}
			}
		}
		else
		{
			foreach (DiscordRole role in memberRoles)
			{
				if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE))
				{
					string[] splitted = oldName.Split(' ');
					if (splitted.Length > 1)
					{
						StringBuilder sb = new();
						for (int i = 0; i < splitted.Length; i++)
						{
							if (i > 0)
							{
								sb.Append(' ');
							}
							if (!splitted[i].Equals(string.Empty) && !splitted[i].Equals(" "))
							{
								sb.Append(splitted[i]);
							}
						}
						returnString = "[" + role.Name + "] " + sb.ToString();
					}
					else
					{
						returnString = "[" + role.Name + "] " + oldName;
					}
				}
			}
		}

		bool isFromNLBE = false;
		foreach (DiscordRole role in member.Roles)
		{
			if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE))
			{
				isFromNLBE = true;
			}
		}
		if (!isFromNLBE)
		{
			if (returnString.StartsWith("[NLBE]") || returnString.StartsWith("[NLBE2]"))
			{
				returnString = returnString.Replace("[NLBE2]", "[]").Replace("[NLBE]", "[]");
			}
		}

		while (returnString.EndsWith(' '))
		{
			returnString = returnString.Remove(returnString.Length - 1);
		}

		return returnString;
	}

	public async Task UpdateUsers()
	{
		DiscordChannel bottestChannel = await _channelService.GetBottestChannel();
		if (bottestChannel != null)
		{
			bool sjtubbersUserNameIsOk = true;
			DiscordMember sjtubbersMember = await bottestChannel.Guild.GetMemberAsync(359817512109604874);
			if (sjtubbersMember.DisplayName != "[NLBE] sjtubbers")
			{
				sjtubbersUserNameIsOk = false;
			}

			if (!sjtubbersUserNameIsOk)
			{
				await _messageService.SendMessage(bottestChannel, await bottestChannel.Guild.GetMemberAsync(Constants.THIBEASTMO_ID), bottestChannel.Guild.Name, "**De bijnaam van sjtubbers was al incorrect dus ben ik gestopt voor ik begon met nakijken van de rest van de bijnamen.**");
				return;
			}
			const int maxMemberChangesAmount = 7;
			IReadOnlyCollection<DiscordMember> members = await bottestChannel.Guild.GetAllMembersAsync();
			Dictionary<DiscordMember, string> memberChanges = [];
			List<DiscordMember> membersNotFound = [];
			List<DiscordMember> correctNicknamesOfMembers = [];
			//test 3x if names are correct
			for (int i = 0; i < 3; i++)
			{
				foreach (DiscordMember member in members)
				{
					if (memberChanges.Count + membersNotFound.Count >= maxMemberChangesAmount)
					{
						break;
					}

					if (!member.IsBot && member.Roles != null && member.Roles.Contains(bottestChannel.Guild.GetRole(Constants.LEDEN_ROLE)))
					{
						bool accountFound = false;
						bool goodClanTag = false;
						Tuple<string, string> gebruiker = GetIGNFromMember(member.DisplayName);
						IReadOnlyList<WGAccount> wgAccounts = await WGAccount.searchByName(SearchAccuracy.EXACT, gebruiker.Item2, _configuration["NLBEBOT:WarGamingAppId"], false, true, false);
						if (wgAccounts != null && wgAccounts.Count > 0)
						{
							//Account met exact deze gebruikersnaam gevonden
							accountFound = true;
							string clanTag = string.Empty;
							if (gebruiker.Item1.Length > 1 && gebruiker.Item1.StartsWith('[') && gebruiker.Item1.EndsWith(']'))
							{
								goodClanTag = true;
								string currentClanTag = string.Empty;
								if (wgAccounts[0].clan != null && wgAccounts[0].clan.tag != null)
								{
									currentClanTag = wgAccounts[0].clan.tag;
								}
								string goodDisplayName = '[' + currentClanTag + "] " + wgAccounts[0].nickname;
								if (wgAccounts[0].nickname != null && !member.DisplayName.Equals(goodDisplayName))
								{
									memberChanges.TryAdd(member, goodDisplayName);
								}
								else if (member.DisplayName.Equals(goodDisplayName))
								{
									correctNicknamesOfMembers.Add(member);
								}
							}
							if (!goodClanTag)
							{
								if (wgAccounts[0].clan != null && wgAccounts[0].clan.tag != null)
								{
									clanTag = wgAccounts[0].clan.tag;
								}
								string goodDisplayName = '[' + clanTag + "] " + wgAccounts[0].nickname;
								memberChanges.TryAdd(member, goodDisplayName);
							}
						}
						if (!accountFound)
						{
							membersNotFound.Add(member);
						}
					}
				}
			}
			IEnumerable<DiscordMember> correctNicknames = correctNicknamesOfMembers.Distinct();
			for (int i = 0; i < memberChanges.Count; i++)
			{
				DiscordMember currentMember = memberChanges.Keys.ElementAt(i);
				if (correctNicknames.Contains(currentMember))
				{
					memberChanges.Remove(currentMember);
					i--;
				}
			}
			for (int i = 0; i < membersNotFound.Count; i++)
			{
				DiscordMember currentMember = membersNotFound[i];
				if (correctNicknames.Contains(currentMember))
				{
					memberChanges.Remove(currentMember);
					i--;
				}
			}
			if (memberChanges.Count + membersNotFound.Count == 0)
			{
				string bericht = "Bijnamen van gebruikers nagekeken maar geen namen moesten aangepast worden.";
				await bottestChannel.SendMessageAsync("**" + bericht + "**");
				_discordClient.Logger.LogInformation(bericht);
			}
			else if (memberChanges.Count + membersNotFound.Count < maxMemberChangesAmount)
			{
				foreach (KeyValuePair<DiscordMember, string> memberChange in memberChanges)
				{
					await _messageService.SendMessage(bottestChannel, await bottestChannel.Guild.GetMemberAsync(Constants.THIBEASTMO_ID), bottestChannel.Guild.Name, "**De bijnaam van **`" + memberChange.Key.DisplayName + "`** wordt aangepast naar **`" + memberChange.Value + "`");
					await ChangeMemberNickname(memberChange.Key, memberChange.Value);
				}
				foreach (DiscordMember memberNotFound in membersNotFound)
				{
					await _messageService.SendMessage(bottestChannel, await bottestChannel.Guild.GetMemberAsync(Constants.THIBEASTMO_ID), bottestChannel.Guild.Name, "**Bijnaam van **`" + memberNotFound.DisplayName + "` (Discord ID: `" + memberNotFound.Id + "`)** komt niet overeen met WoTB account.**");
					await _messageService.SendPrivateMessage(memberNotFound, bottestChannel.Guild.Name, "Hallo,\n\nEr werd voor iedere gebruiker in de NLBE discord server gecontroleerd of je bijnaam overeenkomt met je wargaming account.\nHelaas is dit voor jou niet het geval.\nZou je dit zelf even willen aanpassen aub?\nPas je bijnaam aan naargelang de vereisten het #regels kanaal.\n\nAlvast bedankt!\n- [NLBE] sjtubbers#4241");
				}
			}
			else
			{
				StringBuilder sb = new("Deze spelers hadden een verkeerde bijnaam:\n```");
				if (memberChanges.Count > 0)
				{
					foreach (KeyValuePair<DiscordMember, string> memberChange in memberChanges)
					{
						sb.AppendLine(memberChange.Key.DisplayName + " -> " + memberChange.Value);
					}
					sb.Append("```");
				}
				if (membersNotFound.Count > 0)
				{
					if (sb.Length > 3)
					{
						sb.Append("Deze spelers konden niet gevonden worden:\n```");
					}
					foreach (DiscordMember memberNotFound in membersNotFound)
					{
						sb.AppendLine(memberNotFound.DisplayName);
					}
					sb.Append("```");
				}
				await _messageService.SendMessage(bottestChannel, await bottestChannel.Guild.GetMemberAsync(Constants.THIBEASTMO_ID), bottestChannel.Guild.Name, "**De bijnamen van 7 of meer spelers waren incorrect of niet gevonden dus ben ik gestopt voor ik begon met nakijken van de rest van de bijnamen.\nHier is een lijstje van aanpassingen die zouden gemaakt zijn:**\n" + sb);
			}
		}
	}
	public Tuple<string, string> GetIGNFromMember(string displayName)
	{
		string[] splitted = displayName.Split(']');
		StringBuilder sb = new();
		for (int i = 1; i < splitted.Length; i++)
		{
			if (i > 1)
			{
				sb.Append(' ');
			}
			sb.Append(splitted[i]);
		}
		string clan = string.Empty;
		if (splitted.Length > 1)
		{
			clan = splitted[0] + ']';
		}
		return new Tuple<string, string>(clan, sb.ToString().Trim(' '));
	}
}
