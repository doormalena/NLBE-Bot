namespace NLBE_Bot.Services;

using DiscordHelper;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Net.Models;
using FMWOTB;
using FMWOTB.Account;
using FMWOTB.Clans;
using FMWOTB.Tools;
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

internal class UserService(ILogger<UserService> logger, IOptions<BotOptions> options, IMessageService messageService) : IUserService
{
	private readonly ILogger<UserService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

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
				Value = (discordMember.Username + "#" + discordMember.Discriminator).adaptToDiscordChat(),
				Inline = true
			};
			deflist.Add(newDef1);
			DEF newDef2 = new()
			{
				Name = "Bijnaam",
				Value = discordMember.DisplayName.adaptToDiscordChat(),
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
			newDef4.Value = sbRoles.ToString().adaptToDiscordChat();
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
				Title = "Info over " + discordMember.DisplayName.adaptToDiscordChat() + (discordMember.IsBot ? " [BOT]" : ""),
				Fields = deflist,
				Author = newAuthor,
			};
			await _messageService.CreateEmbed(channel, embedOptions);
		}
		else if (gebruiker is WGAccount account)
		{
			List<DEF> deflist = [];
			WGAccount member = account;
			try
			{
				DEF newDef1 = new()
				{
					Name = "Gebruikersnaam",
					Value = member.nickname.adaptToDiscordChat(),
					Inline = true
				};
				deflist.Add(newDef1);
				if (member.clan != null && member.clan.tag != null)
				{
					DEF newDef2 = new()
					{
						Name = "Clan",
						Value = member.clan.tag.adaptToDiscordChat(),
						Inline = true
					};
					deflist.Add(newDef2);
					DEF newDef4 = new()
					{
						Name = "Rol",
						Value = member.clan.role.ToString().adaptToDiscordChat(),
						Inline = true
					};
					deflist.Add(newDef4);
					DEF newDef5 = new()
					{
						Name = "Clan gejoined op"
					};
					string[] splitted = member.clan.joined_at.Value.ConvertToDate().Split(' ');
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
					Value = member.account_id.ToString(),
					Inline = true
				};
				deflist.Add(newDef3);
				if (member.created_at.HasValue)
				{
					DEF newDef6 = new()
					{
						Name = "Gestart op",
						Value = member.created_at.Value.ConvertToDate(),
						Inline = true
					};
					deflist.Add(newDef6);
				}
				if (member.last_battle_time.HasValue)
				{
					DEF newDef6 = new()
					{
						Name = "Laatst actief",
						Value = member.last_battle_time.Value.ConvertToDate(),
						Inline = true
					};
					deflist.Add(newDef6);
				}
				if (member.statistics != null)
				{
					if (member.statistics.rating != null)
					{
						DEF newDef4 = new()
						{
							Name = "Rating (WR)",
							Value = member.statistics.rating.battles > 0 ? string.Format("{0:.##}", CalculateWinRate(member.statistics.rating.wins, member.statistics.rating.battles)) : "Nog geen rating gespeeld",
							Inline = true
						};
						deflist.Add(newDef4);
					}
					if (member.statistics.all != null)
					{
						DEF newDef5 = new()
						{
							Name = "Winrate",
							Value = string.Format("{0:.##}", CalculateWinRate(member.statistics.all.wins, member.statistics.all.battles)),
							Inline = true
						};
						deflist.Add(newDef5);
						DEF newDef6 = new()
						{
							Name = "Gem. damage",
							Value = (member.statistics.all.damage_dealt / member.statistics.all.battles).ToString(),
							Inline = true
						};
						deflist.Add(newDef6);
						DEF newDef7 = new()
						{
							Name = "Battles",
							Value = member.statistics.all.battles.ToString(),
							Inline = true
						};
						deflist.Add(newDef7);
					}
				}

				EmbedOptions embedOptions = new()
				{
					Title = "Info over " + member.nickname.adaptToDiscordChat(),
					Fields = deflist,
					Color = Constants.BOT_COLOR,
					NextMessage = member.blitzstars
				};

				await _messageService.CreateEmbed(channel, embedOptions);
			}
		}
		else if (gebruiker is DiscordUser discordUser)
		{
			DiscordEmbedBuilder.EmbedAuthor newAuthor = new()
			{
				Name = discordUser.Username.adaptToDiscordChat(),
				IconUrl = discordUser.AvatarUrl
			};

			List<DEF> deflist = [];
			DEF newDef1 = new()
			{
				Name = "Gebruikersnaam",
				Value = discordUser.Username.adaptToDiscordChat(),
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
						Value = (discordUser.Presence.Activity.CustomStatus.Emoji != null ? discordUser.Presence.Activity.CustomStatus.Emoji.Name : string.Empty) + discordUser.Presence.Activity.CustomStatus.Name.adaptToDiscordChat(),
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
						Value = sb.ToString().adaptToDiscordChat(),
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
				Title = "Info over " + discordUser.Username.adaptToDiscordChat() + "#" + discordUser.Discriminator + (discordUser.IsBot ? " [BOT]" : ""),
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
						sbs[columnCounter].AppendLine(memberList[0].DisplayName.adaptToDiscordChat());
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
					string defValue = item.ToString().adaptToDiscordChat();
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
						Name = defName.adaptToDiscordChat(),
						Value = defValue
					};
					deflist.Add(newDef);
				}
			}
			return deflist;
		}
		return [];
	}

	public async Task<List<DEF>> ListInPlayerEmbed(int columns, List<Members> memberList, string searchTerm, IDiscordGuild guild)
	{
		if (memberList.Count == 0)
		{
			return [];
		}

		List<string> nameList;

		if (searchTerm.Contains('d'))
		{
			List<WGAccount> wgAccountList = memberList.Select(member => new WGAccount(_options.WarGamingAppId, member.account_id, false, false, false))
													  .OrderBy(p => p.last_battle_time).Reverse().ToList();

			nameList = wgAccountList.Select(member => member.nickname).ToList();
		}
		else
		{
			nameList = memberList.Select(member => member.account_name).ToList();
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
						sbs[columnCounter].AppendLine("**" + nameList[0].adaptToDiscordChat() + "**");
					}
				}
				else
				{
					sbs[columnCounter].AppendLine(nameList[0].adaptToDiscordChat());
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
					Name = defName.adaptToDiscordChat(),
					Value = item.ToString()
				};
				deflist.Add(newDef);
			}
		}
		return deflist;
	}
	public async Task<WGAccount> SearchPlayer(IDiscordChannel channel, IDiscordMember member, IDiscordUser user, string guildName, string naam)
	{
		try
		{
			IReadOnlyList<WGAccount> searchResults = await WGAccount.searchByName(SearchAccuracy.STARTS_WITH_CASE_INSENSITIVE, naam, _options.WarGamingAppId, false, false, true);
			StringBuilder sb = new();
			int index = 0;
			if (searchResults != null)
			{
				if (searchResults.Count > 1)
				{
					int counter = 0;
					foreach (WGAccount account in searchResults)
					{
						counter++;
						sb.AppendLine(counter + ". " + account.nickname.adaptToDiscordChat());
					}
					index = await _messageService.WaitForReply(channel, user, sb.ToString(), searchResults.Count);
				}
				if (index >= 0 && searchResults.Count >= 1)
				{
					WGAccount account = new(_options.WarGamingAppId, searchResults[index].account_id, false, true, true);
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
				await _messageService.SendMessage(channel, member, guildName, "**Gebruiker (**`" + naam.adaptToDiscordChat() + "`**) kon niet gevonden worden!**");
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
