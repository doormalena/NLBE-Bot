namespace NLBE_Bot.Services;

using DiscordHelper;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Net.Models;
using FMWOTB;
using FMWOTB.Clans;
using FMWOTB.Exceptions;
using FMWOTB.Interfaces;
using FMWOTB.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

internal class UserService(ILogger<UserService> logger,
						   IMessageService messageService,
						   IAccountsRepository accountRepository,
						   IClansRepository clanRepository) : IUserService
{
	private readonly ILogger<UserService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
	private readonly IAccountsRepository _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
	private readonly IClansRepository _clanRepository = clanRepository ?? throw new ArgumentNullException(nameof(clanRepository));

	public async Task<IDiscordMember> GetDiscordMember(IDiscordGuild guild, ulong userID)
	{
		return await guild.GetMemberAsync(userID);
	}

	public async Task ChangeMemberNickname(IDiscordMember member, string nickname)
	{
		try
		{
			void mem(MemberEditModel item)
			{
				item.Nickname = nickname;
				item.AuditLogReason = "Changed by NLBE-Bot in compliance with the server rules.";
			}
			await member.ModifyAsync(mem);
		}
		catch (UnauthorizedException ex)
		{
			throw new UnauthorizedAccessException("Failed to update member nickname due to insufficient permissions.", ex);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to update nickname for {Username} to `{Nickname}`", member.Username, nickname);
		}
	}

	public string UpdateName(IDiscordMember member, string oldName)
	{
		string returnString = oldName;
		IEnumerable<IDiscordRole> memberRoles = member.Roles;
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

				foreach (IDiscordRole role in memberRoles)
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
				foreach (IDiscordRole role in memberRoles)
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
			foreach (IDiscordRole role in memberRoles)
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
			foreach (IDiscordRole role in memberRoles)
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
			foreach (IDiscordRole role in memberRoles)
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
		foreach (IDiscordRole role in member.Roles)
		{
			if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE))
			{
				isFromNLBE = true;
			}
		}
		if (!isFromNLBE && (returnString.StartsWith("[NLBE]") || returnString.StartsWith("[NLBE2]")))
		{
			returnString = returnString.Replace("[NLBE2]", "[]").Replace("[NLBE]", "[]");
		}

		while (returnString.EndsWith(' '))
		{
			returnString = returnString.Remove(returnString.Length - 1);
		}

		return returnString;
	}

	public WotbPlayerNameInfo GetWotbPlayerNameFromDisplayName(string displayName)
	{
		// Pattern: optional [CLAN] (with or without space), then player name
		// Examples matched: "[TAG] Player", "[NLBE]John", "NoClanTagName"
		Match match = Regex.Match(displayName, @"^(?:\[(?<clan>[^\]]+)\]\s*)?(?<player>.+)$", RegexOptions.NonBacktracking);
		if (match.Success)
		{
			string clanTag = match.Groups["clan"].Success ? $"[{match.Groups["clan"].Value}]" : string.Empty;
			string playerName = match.Groups["player"].Value.Trim();
			return new WotbPlayerNameInfo(clanTag, playerName);
		}

		// Fallback: no match, treat whole as player name
		return new WotbPlayerNameInfo(string.Empty, displayName.Trim());
	}

	public async Task ShowMemberInfo(IDiscordChannel channel, object gebruiker)
	{
		if (gebruiker is IDiscordMember discordMember)
		{
			DiscordEmbedBuilder.EmbedAuthor newAuthor = new()
			{
				Name = discordMember.Username.Replace('_', '▁'),
				IconUrl = discordMember.AvatarUrl
			};

			List<DEF> deflist = [];
			DEF newDef1 = new()
			{
				Name = "Gebruiker",
				Value = (discordMember.Username + "#" + discordMember.Discriminator).AdaptToDiscordChat(),
				Inline = true
			};
			deflist.Add(newDef1);
			DEF newDef2 = new()
			{
				Name = "Bijnaam",
				Value = discordMember.DisplayName.AdaptToDiscordChat(),
				Inline = true
			};
			deflist.Add(newDef2);
			DEF newDef3 = new()
			{
				Name = "GebruikersID",
				Value = discordMember.Id.ToString(),
				Inline = true
			};
			deflist.Add(newDef3);
			DEF newDef4 = new()
			{
				Name = "Rol" + (discordMember.Roles.Count() > 1 ? "len" : string.Empty)
			};
			StringBuilder sbRoles = new();
			bool firstTime = true;
			foreach (IDiscordRole role in discordMember.Roles)
			{
				if (firstTime)
				{
					firstTime = false;
				}
				else
				{
					sbRoles.Append(", ");
				}
				sbRoles.Append(role.Name.Replace('_', '▁'));
			}
			if (sbRoles.Length == 0)
			{
				sbRoles.Append("`Had geen rol`");
			}
			newDef4.Value = sbRoles.ToString().AdaptToDiscordChat();
			newDef4.Inline = true;
			deflist.Add(newDef4);
			DEF newDef5 = new()
			{
				Name = "Gejoined op"
			};
			string[] splitted = discordMember.JoinedAt.ConvertToDate().Split(' ');
			newDef5.Value = splitted[0] + " " + splitted[1];
			newDef5.Inline = true;
			deflist.Add(newDef5);
			if (discordMember.Presence != null)
			{
				StringBuilder sb = new();
				sb.AppendLine(discordMember.Presence.Status.ToString());
				if (discordMember.Presence.Activity != null && discordMember.Presence.Activity.CustomStatus != null && discordMember.Presence.Activity.CustomStatus.Name != null)
				{
					sb.AppendLine(discordMember.Presence.Activity.CustomStatus.Name);
				}
				DEF newDef6 = new()
				{
					Name = "Status",
					Value = sb.ToString(),
					Inline = true
				};
				deflist.Add(newDef6);
			}
			if (discordMember.Verified.HasValue && !discordMember.Verified.Value)
			{
				DEF newDef6 = new()
				{
					Name = "Niet bevestigd!",
					Value = "Dit account is niet bevestigd!",
					Inline = true
				};
				deflist.Add(newDef6);
			}

			EmbedOptions embedOptions = new()
			{
				Title = "Info over " + discordMember.DisplayName.AdaptToDiscordChat() + (discordMember.IsBot ? " [BOT]" : ""),
				Fields = deflist,
				Author = newAuthor,
			};
			await _messageService.CreateEmbed(channel, embedOptions);
		}
		else if (gebruiker is WotbAccountInfo account)
		{
			WotbClanInfo clanInfo = null;
			if (account.ClanId > 0)
			{
				clanInfo = await _clanRepository.GetByIdAsync(account.ClanId.Value);
			}

			List<DEF> deflist = [];
			try
			{
				WotbClanMember clanMember = clanInfo?.Members.FirstOrDefault(m => m.AccountId == account.AccountId);

				DEF newDef1 = new()
				{
					Name = "Gebruikersnaam",
					Value = account.Nickname.AdaptToDiscordChat(),
					Inline = true
				};
				deflist.Add(newDef1);
				if (clanInfo != null && clanInfo.Tag != null)
				{
					DEF newDef2 = new()
					{
						Name = "Clan",
						Value = clanInfo.Tag.AdaptToDiscordChat(),
						Inline = true
					};
					deflist.Add(newDef2);
					DEF newDef4 = new()
					{
						Name = "Rol",
						Value = clanMember?.Role.ToString().AdaptToDiscordChat(),
						Inline = true
					};
					deflist.Add(newDef4);
					DEF newDef5 = new()
					{
						Name = "Clan gejoined op"
					};
					string[] splitted = clanMember?.JoinedAt.Value.ConvertToDate().Split(' ');
					newDef5.Value = splitted[0] + " " + splitted[1];
					newDef5.Inline = true;
					deflist.Add(newDef5);
				}
			}
			catch (Exception ex)
			{
				_logger.LogDebug(ex, ex.Message);
			}
			finally
			{
				DEF newDef3 = new()
				{
					Name = "SpelerID",
					Value = account.AccountId.ToString(),
					Inline = true
				};
				deflist.Add(newDef3);
				if (account.CreatedAt.HasValue)
				{
					DEF newDef6 = new()
					{
						Name = "Gestart op",
						Value = account.CreatedAt.Value.ConvertToDate(),
						Inline = true
					};
					deflist.Add(newDef6);
				}
				if (account.LastBattleTime.HasValue)
				{
					DEF newDef6 = new()
					{
						Name = "Laatst actief",
						Value = account.LastBattleTime.Value.ConvertToDate(),
						Inline = true
					};
					deflist.Add(newDef6);
				}
				if (account.Statistics != null)
				{
					if (account.Statistics.rating != null)
					{
						DEF newDef4 = new()
						{
							Name = "Rating (WR)",
							Value = account.Statistics.rating.battles > 0 ? string.Format("{0:.##}", CalculateWinRate(account.Statistics.rating.wins, account.Statistics.rating.battles)) : "Nog geen rating gespeeld",
							Inline = true
						};
						deflist.Add(newDef4);
					}
					if (account.Statistics.all != null)
					{
						DEF newDef5 = new()
						{
							Name = "Winrate",
							Value = string.Format("{0:.##}", CalculateWinRate(account.Statistics.all.wins, account.Statistics.all.battles)),
							Inline = true
						};
						deflist.Add(newDef5);
						DEF newDef6 = new()
						{
							Name = "Gem. damage",
							Value = (account.Statistics.all.damage_dealt / account.Statistics.all.battles).ToString(),
							Inline = true
						};
						deflist.Add(newDef6);
						DEF newDef7 = new()
						{
							Name = "Battles",
							Value = account.Statistics.all.battles.ToString(),
							Inline = true
						};
						deflist.Add(newDef7);
					}
				}

				EmbedOptions embedOptions = new()
				{
					Title = "Info over " + account.Nickname.AdaptToDiscordChat(),
					Fields = deflist,
					Color = Constants.BOT_COLOR,
					NextMessage = account.BlitzStars
				};

				await _messageService.CreateEmbed(channel, embedOptions);
			}
		}
		else if (gebruiker is DiscordUser discordUser)
		{
			DiscordEmbedBuilder.EmbedAuthor newAuthor = new()
			{
				Name = discordUser.Username.AdaptToDiscordChat(),
				IconUrl = discordUser.AvatarUrl
			};

			List<DEF> deflist = [];
			DEF newDef1 = new()
			{
				Name = "Gebruikersnaam",
				Value = discordUser.Username.AdaptToDiscordChat(),
				Inline = true
			};
			deflist.Add(newDef1);
			DEF newDef3 = new()
			{
				Name = "GebruikersID",
				Value = discordUser.Id.ToString(),
				Inline = true
			};
			deflist.Add(newDef3);
			DEF newDef4 = new()
			{
				Name = "Gecreëerd op"
			};
			string[] splitted = discordUser.CreationTimestamp.ConvertToDate().Split(' ');
			newDef4.Value = splitted[0] + " " + splitted[1];
			newDef4.Inline = true;
			deflist.Add(newDef4);
			if (discordUser.Flags.HasValue && !discordUser.Flags.Value.ToString().Equals("None"))
			{
				DEF newDef2 = new()
				{
					Name = "Discord Medailles",
					Value = discordUser.Flags.Value.ToString(),
					Inline = true
				};
				deflist.Add(newDef2);
			}
			if (discordUser.Email != null && discordUser.Email.Length > 0)
			{
				DEF newDef5 = new()
				{
					Name = "E-mail",
					Value = discordUser.Email,
					Inline = true
				};
				deflist.Add(newDef5);
			}
			if (discordUser.Locale != null && discordUser.Locale.Length > 0)
			{
				DEF newDef5 = new()
				{
					Name = "Taal",
					Value = discordUser.Locale,
					Inline = true
				};
				deflist.Add(newDef5);
			}
			if (discordUser.Presence != null)
			{
				if (discordUser.Presence.Activity != null && discordUser.Presence.Activity.CustomStatus != null && discordUser.Presence.Activity.CustomStatus.Name != null)
				{
					DEF newDef7 = new()
					{
						Name = "Custom status",
						Value = (discordUser.Presence.Activity.CustomStatus.Emoji != null ? discordUser.Presence.Activity.CustomStatus.Emoji.Name : string.Empty) + discordUser.Presence.Activity.CustomStatus.Name.AdaptToDiscordChat(),
						Inline = true
					};
					deflist.Add(newDef7);
				}
				if (discordUser.Presence.Activities != null && discordUser.Presence.Activities.Count > 0)
				{
					StringBuilder sb = new();
					foreach (DiscordActivity item in discordUser.Presence.Activities)
					{
						string temp = string.Empty;
						bool customStatus = false;
						if (item.CustomStatus != null && item.CustomStatus.Name.Length > 0)
						{
							customStatus = true;
							temp = (item.CustomStatus.Emoji != null ? item.CustomStatus.Emoji.Name : string.Empty) + item.CustomStatus.Name;
						}
						if (!customStatus)
						{
							temp = item.Name;
						}
						bool streaming = false;
						if (item.StreamUrl != null && item.StreamUrl.Length > 0)
						{
							streaming = true;
							sb.AppendLine("[" + temp + "](" + item.StreamUrl + ")");
						}
						if (!streaming)
						{
							sb.AppendLine(temp);
						}
					}
					DEF newDef7 = new()
					{
						Name = "Recente activiteiten",
						Value = sb.ToString().AdaptToDiscordChat(),
						Inline = true
					};
					deflist.Add(newDef7);
				}
				if (discordUser.Presence.Activity != null && discordUser.Presence.Activity.Name != null)
				{
					string temp = string.Empty;
					bool customStatus = false;
					if (discordUser.Presence.Activity.CustomStatus != null && discordUser.Presence.Activity.CustomStatus.Name.Length > 0)
					{
						customStatus = true;
						temp = (discordUser.Presence.Activity.CustomStatus.Emoji != null ? discordUser.Presence.Activity.CustomStatus.Emoji.Name : string.Empty) + discordUser.Presence.Activity.CustomStatus.Name;
					}
					if (!customStatus)
					{
						bool streaming = false;
						if (discordUser.Presence.Activity.StreamUrl != null && discordUser.Presence.Activity.StreamUrl.Length > 0)
						{
							streaming = true;
							temp = "[" + discordUser.Presence.Activity.Name + "](" + discordUser.Presence.Activity.StreamUrl + ")";
						}
						if (!streaming)
						{
							temp = discordUser.Presence.Activity.Name;
						}
					}
					DEF newDefx = new()
					{
						Name = "Activiteit",
						Value = temp,
						Inline = true
					};
					deflist.Add(newDefx);
				}
				if (discordUser.Presence.ClientStatus != null)
				{
					StringBuilder sb = new();

					if (discordUser.Presence.ClientStatus.Desktop.HasValue)
					{
						sb.AppendLine("Desktop");
					}

					if (discordUser.Presence.ClientStatus.Mobile.HasValue)
					{
						sb.AppendLine("Mobiel");
					}

					if (discordUser.Presence.ClientStatus.Web.HasValue)
					{
						sb.AppendLine("Web");
					}

					if (sb.Length > 0)
					{
						DEF newDef7 = new()
						{
							Name = "Op discord via",
							Value = sb.ToString(),
							Inline = true
						};
						deflist.Add(newDef7);
					}
				}
			}
			if (discordUser.PremiumType.HasValue)
			{
				DEF newDef7 = new()
				{
					Name = "Premiumtype",
					Value = discordUser.PremiumType.Value.ToString(),
					Inline = true
				};
				deflist.Add(newDef7);
			}
			if (discordUser.Verified.HasValue && !discordUser.Verified.Value)
			{
				DEF newDef6 = new()
				{
					Name = "Niet bevestigd!",
					Value = "Dit account is niet bevestigd!",
					Inline = true
				};
				deflist.Add(newDef6);
			}

			EmbedOptions embedOptions = new()
			{
				Title = "Info over " + discordUser.Username.AdaptToDiscordChat() + "#" + discordUser.Discriminator + (discordUser.IsBot ? " [BOT]" : ""),
				Fields = deflist,
				Author = newAuthor,
			};
			await _messageService.CreateEmbed(channel, embedOptions);
		}
	}
	public List<DEF> ListInMemberEmbed(int columns, List<IDiscordMember> memberList, string searchTerm)
	{
		List<IDiscordMember> backupMemberList = [];
		backupMemberList.AddRange(memberList);
		List<StringBuilder> sbs = [];
		for (int i = 0; i < columns; i++)
		{
			sbs.Add(new StringBuilder());
		}
		int counter = 0;
		int columnCounter = 0;
		int rest = memberList.Count % columns;
		int membersPerColumn = (memberList.Count - rest) / columns;
		int amountOfMembers = memberList.Count;
		if (amountOfMembers > 0)
		{
			while (memberList.Count > 0)
			{
				try
				{
					if (searchTerm.ToLower().Contains('b'))
					{
						sbs[columnCounter].AppendLine(memberList[0].DisplayName.AdaptToDiscordChat());
					}
					else
					{
						sbs[columnCounter].AppendLine(memberList[0].Username + "#" + memberList[0].Discriminator);
					}

					//hier

					memberList.RemoveAt(0);

					if (counter == membersPerColumn + (columnCounter == columns - 1 ? rest : 0) || memberList.Count == 1)
					{
						if (columnCounter < columns - 1)
						{
							columnCounter++;
						}
						counter = 0;
					}
					else
					{
						counter++;
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error while processing member list for embed.");
				}
			}
			List<DEF> deflist = [];
			bool firstTime = true;
			foreach (StringBuilder item in sbs)
			{
				if (item.Length > 0)
				{
					string defValue = item.ToString().AdaptToDiscordChat();
					if (defValue.Length > 1024)
					{
						return ListInMemberEmbed(columns + 1, backupMemberList, searchTerm);
					}
					string[] splitted = item.ToString().Split(Environment.NewLine);
					string firstChar = splitted[0].RemoveSyntax().Substring(0, 1);
					string lastChar = string.Empty;
					string defName = string.Empty;
					if (searchTerm.Contains('o') || searchTerm.Contains('c'))
					{
						if (firstTime)
						{
							firstTime = false;
							defName = "Recentst";
						}
						else
						{
							defName = item.Equals(sbs[sbs.Count - 1]) ? "minst recent" : "minder recent";
						}
					}
					else
					{
						defName = firstChar.ToUpper() + (splitted.Length > 2 ? " - " + lastChar.ToUpper() : "");
					}

					DEF newDef = new()
					{
						Inline = true,
						Name = defName.AdaptToDiscordChat(),
						Value = defValue
					};
					deflist.Add(newDef);
				}
			}
			return deflist;
		}
		return [];
	}

	public async Task<List<DEF>> ListInPlayerEmbed(int columns, List<WotbClanMember> memberList, string searchTerm, IDiscordGuild guild)
	{
		if (memberList.Count == 0)
		{
			return [];
		}

		List<string> nameList;

		if (searchTerm.Contains('d'))
		{
			IEnumerable<Task<WotbAccountInfo>> tasks = memberList.Select(member => _accountRepository.GetByIdAsync(member.AccountId));
			List<WotbAccountInfo> wgAccountList = [.. (await Task.WhenAll(tasks)).OrderByDescending(p => p.LastBattleTime)];

			nameList = [.. wgAccountList.Select(member => member.Nickname)];
		}
		else
		{
			nameList = [.. memberList.Select(member => member.AccountName)];
		}

		List<StringBuilder> sbs = [];
		for (int i = 0; i < columns; i++)
		{
			sbs.Add(new StringBuilder());
		}

		int counter = 0;
		int columnCounter = 0;
		int rest = nameList.Count % columns;
		int membersPerColumn = (nameList.Count - rest) / columns;

		IReadOnlyCollection<IDiscordMember> members = [];
		if (searchTerm.Contains('s'))
		{
			members = await guild.GetAllMembersAsync();
		}
		while (nameList.Count > 0)
		{
			try
			{
				if (searchTerm.Contains('s'))
				{
					bool found = false;
					foreach (IDiscordMember memberx in members)
					{
						string[] splittedName = memberx.DisplayName.Split(']');
						if (splittedName.Length > 1)
						{
							string tempName = splittedName[1].Trim(' ');
							if (tempName.ToLower().Equals(nameList[0].ToLower()))
							{
								sbs[columnCounter].AppendLine("`" + nameList[0] + "`");
								found = true;
								break;
							}
						}
					}
					if (!found)
					{
						sbs[columnCounter].AppendLine("**" + nameList[0].AdaptToDiscordChat() + "**");
					}
				}
				else
				{
					sbs[columnCounter].AppendLine(nameList[0].AdaptToDiscordChat());
				}

				nameList.RemoveAt(0);

				if (counter == membersPerColumn + (columnCounter == columns - 1 ? rest : 0) || nameList.Count == 1)
				{
					if (columnCounter < columns - 1)
					{
						columnCounter++;
					}
					counter = 0;
				}
				else
				{
					counter++;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error while processing member list for embed.");
			}
		}

		List<DEF> deflist = [];
		bool firstTime = true;
		foreach (StringBuilder item in sbs)
		{
			if (item.Length > 0)
			{
				string[] splitted = item.ToString().Split(Environment.NewLine);
				string firstChar = splitted[0].RemoveSyntax().Substring(0, 1);
				string lastChar = string.Empty;
				for (int i = splitted.Length - 1; i > 0; i--)
				{
					if (splitted[i] != string.Empty)
					{
						lastChar = splitted[i].RemoveSyntax().ToUpper().First().ToString();
						break;
					}
				}
				string defName = string.Empty;
				if (searchTerm.Contains('d'))
				{
					if (firstTime)
					{
						firstTime = false;
						defName = "Recentst";
					}
					else
					{
						defName = item.Equals(sbs[sbs.Count - 1]) ? "minst recent" : "minder recent";
					}
				}
				else
				{
					defName = firstChar.ToUpper() + (splitted.Length > 2 ? " - " + lastChar.ToUpper() : "");
				}
				DEF newDef = new()
				{
					Inline = true,
					Name = defName.AdaptToDiscordChat(),
					Value = item.ToString()
				};
				deflist.Add(newDef);
			}
		}
		return deflist;
	}
	public async Task<WotbAccountInfo> SearchPlayer(IDiscordChannel channel, IDiscordMember member, IDiscordUser user, string guildName, string naam)
	{
		try
		{
			IReadOnlyList<WotbAccountListItem> searchResults = await _accountRepository.SearchByNameAsync(SearchType.StartsWith, naam); // TODO: missing clan members and statistics
			StringBuilder sb = new();
			int index = 0;

			if (searchResults != null)
			{
				if (searchResults.Count > 1)
				{
					int counter = 0;
					foreach (WotbAccountListItem account in searchResults)
					{
						counter++;
						sb.AppendLine(counter + ". " + account.Nickname.AdaptToDiscordChat());
					}
					index = await _messageService.WaitForReply(channel, user, sb.ToString(), searchResults.Count);
				}

				if (index >= 0 && searchResults.Count >= 1)
				{
					WotbAccountInfo account = await _accountRepository.GetByIdAsync(searchResults[index].AccountId); // TODO: missing clan and statistics
					await ShowMemberInfo(channel, account);
					return account;
				}
				else
				{
					await _messageService.SendMessage(channel, member, guildName, "**Gebruiker (**`" + naam + "`**) kon niet gevonden worden!**");
				}
			}
			else
			{
				await _messageService.SendMessage(channel, member, guildName, "**Gebruiker (**`" + naam.AdaptToDiscordChat() + "`**) kon niet gevonden worden!**");
			}
		}
		catch (TooManyResultsException ex)
		{
			_logger.LogWarning("While searching for player by name: {Message}", ex.Message);
			await _messageService.SendMessage(channel, member, guildName, "**Te veel resultaten waren gevonden, wees specifieker!**");
		}
		return null;
	}

	private static double CalculateWinRate(int wins, int battles)
	{
		return wins / battles * 100;
	}
}
