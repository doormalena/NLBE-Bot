namespace NLBE_Bot.EventHandlers;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using FMWOTB.Account;
using FMWOTB.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLBE_Bot;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class GuildMemberEventHandler(IErrorHandler errorHandler, ILogger<GuildMemberEventHandler> logger, IConfiguration configuration,
									IBotState botState, IChannelService channelService, IGuildProvider guildProvider, IUserService userService, IMessageService messageService) : IGuildMemberEventHandler
{
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly ILogger<GuildMemberEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	private readonly IBotState _botState = botState ?? throw new ArgumentNullException(nameof(botState));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IGuildProvider _guildProvider = guildProvider ?? throw new ArgumentNullException(nameof(guildProvider));
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

	public void Register(IDiscordClient client)
	{
		client.GuildMemberAdded += OnMemberAdded;
		client.GuildMemberUpdated += OnMemberUpdated;
		client.GuildMemberRemoved += OnMemberRemoved;
	}

	internal async Task OnMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
	{
		if (_botState.IgnoreEvents)
		{
			return;
		}

		if (e.Guild.Id == Constants.NLBE_SERVER_ID)
		{
			DiscordRole noobRole = e.Guild.GetRole(Constants.NOOB_ROLE);
			if (noobRole != null)
			{
				await e.Member.GrantRoleAsync(noobRole);

				DiscordChannel welkomChannel = await _channelService.GetWelkomChannel();

				if (welkomChannel != null)
				{
					DiscordChannel regelsChannel = await _channelService.GetRegelsChannel();

					welkomChannel.SendMessageAsync(e.Member.Mention + " welkom op de NLBE discord server. Beantwoord eerst de vraag en lees daarna de " + (regelsChannel != null ? regelsChannel.Mention : "#regels") + " aub.").Wait();
					DiscordGuild guild = _guildProvider.GetGuild(e.Guild.Id).Result;

					if (guild != null)
					{
						DiscordUser user = await sender.GetUserAsync(e.Member.Id);

						if (user != null)
						{
							IReadOnlyList<WGAccount> searchResults = [];
							bool resultFound = false;
							StringBuilder sbDescription = new();
							int counter = 0;
							bool firstTime = true;

							while (!resultFound)
							{
								string question = user.Mention + " Wat is je gebruikersnaam van je wargaming account?";
								if (firstTime)
								{
									firstTime = false;
								}
								else
								{
									question = "**We konden dit Wargamingaccount niet vinden, probeer opnieuw! (Hoofdlettergevoelig)**\n" + question;
								}
								string ign = await _messageService.AskQuestion(welkomChannel, user, guild, question);
								searchResults = await WGAccount.searchByName(SearchAccuracy.EXACT, ign, _configuration["NLBEBOT:WarGamingAppId"], false, true, false);
								if (searchResults != null && searchResults.Count > 0)
								{
									resultFound = true;
									foreach (WGAccount tempAccount in searchResults)
									{
										string tempClanName = string.Empty;
										if (tempAccount.clan != null)
										{
											tempClanName = tempAccount.clan.tag;
										}
										try
										{
											sbDescription.AppendLine(++counter + ". " + tempAccount.nickname + " " + (tempClanName.Length > 0 ? '`' + tempClanName + '`' : string.Empty));
										}
										catch (Exception ex)
										{
											_logger.LogWarning(ex, "Error while looking for basicInfo for {Ign}:\n {StackTrace}", ign, ex.StackTrace);
										}
									}
								}
							}

							int selectedAccount = 0;
							if (searchResults.Count > 1)
							{
								selectedAccount = -1;
								while (selectedAccount == -1)
								{
									selectedAccount = await _messageService.WaitForReply(welkomChannel, user, sbDescription.ToString(), counter);
								}
							}

							WGAccount account = searchResults[selectedAccount];

							string clanName = string.Empty;
							if (account.clan != null && account.clan.tag != null)
							{
								if (account.clan.clan_id.Equals(Constants.NLBE_CLAN_ID) || account.clan.clan_id.Equals(Constants.NLBE2_CLAN_ID))
								{
									await e.Member.SendMessageAsync("Indien je echt van **" + account.clan.tag + "** bent dan moet je even vragen of iemand jouw de **" + account.clan.tag + "** rol wilt geven.");
								}
								else
								{
									clanName = account.clan.tag;
								}
							}
							_userService.ChangeMemberNickname(e.Member, "[" + clanName + "] " + account.nickname).Wait();
							await e.Member.SendMessageAsync("We zijn er bijna. Als je nog even de regels wilt lezen in **#regels** dan zijn we klaar.");
							DiscordRole rulesNotReadRole = e.Guild.GetRole(Constants.MOET_REGELS_NOG_LEZEN_ROLE);
							if (rulesNotReadRole != null)
							{
								await e.Member.RevokeRoleAsync(noobRole);
								await e.Member.GrantRoleAsync(rulesNotReadRole);
							}
							IReadOnlyCollection<DiscordMember> allMembers = await e.Guild.GetAllMembersAsync();
							bool atLeastOneOtherPlayerWithNoobRole = false;
							foreach (DiscordMember aMember in allMembers)
							{
								if (aMember.Roles.Contains(noobRole))
								{
									atLeastOneOtherPlayerWithNoobRole = true;
									break;
								}
							}
							if (atLeastOneOtherPlayerWithNoobRole)
							{
								await _channelService.CleanWelkomChannel(e.Member.Id);
							}
							else
							{
								await _channelService.CleanWelkomChannel();
							}
						}
					}
				}
			}
			else
			{
				await _errorHandler.HandleErrorAsync("Could not grant new member[" + e.Member.DisplayName + " (" + e.Member.Username + "#" + e.Member.Discriminator + ")] the Noob role.");
			}
		}
	}

	internal async Task OnMemberUpdated(DiscordClient _, GuildMemberUpdateEventArgs e)
	{
		if (botState.IgnoreEvents)
		{
			return;
		}

		foreach (KeyValuePair<ulong, DiscordGuild> guild in _guildProvider.Guilds.Where(g => g.Key != Constants.NLBE_SERVER_ID))
		{
			DiscordMember member = await _userService.GetDiscordMember(guild.Value, e.Member.Id);

			if (member == null)
			{
				continue;
			}

			IEnumerable<DiscordRole> userRoles = member.Roles;
			bool isNoob = userRoles.Any(role => role.Id.Equals(Constants.NOOB_ROLE));
			bool hasRoles = userRoles.Any();

			if (!isNoob && hasRoles && (e.RolesAfter != null || !string.IsNullOrEmpty(e.NicknameAfter)))
			{
				string editedName = _userService.UpdateName(member, member.DisplayName);
				if (!editedName.Equals(member.DisplayName, StringComparison.Ordinal) && !string.IsNullOrEmpty(editedName))
				{
					await _userService.ChangeMemberNickname(member, editedName);
				}
			}
		}
	}

	internal async Task OnMemberRemoved(DiscordClient _, GuildMemberRemoveEventArgs e)
	{
		if (_botState.IgnoreEvents)
		{
			return;
		}

		if (e.Member.Id.Equals(Constants.THIBEASTMO_ALT_ID) || !e.Guild.Id.Equals(Constants.NLBE_SERVER_ID))
		{
			return;
		}

		DiscordChannel oudLedenChannel = await _channelService.GetOudLedenChannel();
		if (oudLedenChannel != null)
		{
			IReadOnlyDictionary<ulong, DiscordRole> serverRoles = null;
			foreach (KeyValuePair<ulong, DiscordGuild> guild in _guildProvider.Guilds)
			{
				if (guild.Value.Id.Equals(Constants.NLBE_SERVER_ID))
				{
					serverRoles = guild.Value.Roles;
				}
			}
			if (serverRoles != null && serverRoles.Count > 0)
			{
				IEnumerable<DiscordRole> memberRoles = e.Member.Roles;
				StringBuilder sbRoles = new();
				bool firstRole = true;
				foreach (DiscordRole role in memberRoles)
				{
					foreach (KeyValuePair<ulong, DiscordRole> serverRole in serverRoles)
					{
						if (serverRole.Key.Equals(role.Id))
						{
							if (role.Id.Equals(Constants.NOOB_ROLE))
							{
								await _channelService.CleanWelkomChannel();
							}
							if (firstRole)
							{
								firstRole = false;
							}
							else
							{
								sbRoles.Append(", ");
							}
							sbRoles.Append(role.Name);
						}
					}
				}
				List<DEF> defList = [];
				DEF newDef1 = new()
				{
					Inline = true,
					Name = "Bijnaam:",
					Value = e.Member.DisplayName
				};
				defList.Add(newDef1);
				DEF newDef2 = new()
				{
					Inline = true,
					Name = "Gebruiker:",
					Value = e.Member.Username + "#" + e.Member.Discriminator
				};
				defList.Add(newDef2);
				DEF newDef3 = new()
				{
					Inline = true,
					Name = "GebruikersID:",
					Value = e.Member.Id.ToString()
				};
				defList.Add(newDef3);
				if (sbRoles.Length > 0)
				{
					DEF newDef = new()
					{
						Inline = true,
						Name = "Rollen:",
						Value = sbRoles.ToString()
					};
					defList.Add(newDef);
				}

				EmbedOptions options = new()
				{
					Title = e.Member.Username + " heeft de server verlaten",
					Fields = defList,
				};

				await _messageService.CreateEmbed(oudLedenChannel, options);
			}
		}
	}
}
