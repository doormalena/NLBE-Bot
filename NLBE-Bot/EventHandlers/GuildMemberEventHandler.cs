namespace NLBE_Bot.EventHandlers;

using DSharpPlus;
using DSharpPlus.EventArgs;
using FMWOTB.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class GuildMemberEventHandler(IErrorHandler errorHandler,
									   ILogger<GuildMemberEventHandler> logger,
									   IOptions<BotOptions> options,
									   IChannelService channelService,
									   IUserService userService,
									   IMessageService messageService,
									   IWGAccountService wgAccountService) : IGuildMemberEventHandler
{
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly ILogger<GuildMemberEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
	private readonly IWGAccountService _wgAccountService = wgAccountService ?? throw new ArgumentNullException(nameof(wgAccountService));

	private IBotState _botState;

	public void Register(IDiscordClient client, IBotState botState)
	{
		_ = client ?? throw new ArgumentNullException(nameof(client));
		_botState = botState ?? throw new ArgumentNullException(nameof(botState));

		client.GuildMemberAdded += OnMemberAdded;
		client.GuildMemberUpdated += OnMemberUpdated;
		client.GuildMemberRemoved += OnMemberRemoved;
	}

	private async Task OnMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
	{
		await HandleMemberAdded(new DiscordClientWrapper(sender), new DiscordGuildWrapper(e.Guild), new DiscordMemberWrapper(e.Member));
	}
	private async Task OnMemberUpdated(DiscordClient _, GuildMemberUpdateEventArgs e)
	{
		await HandleMemberUpdated(new DiscordGuildWrapper(e.Guild), new DiscordMemberWrapper(e.Member), e.RolesAfter.Select(r => (IDiscordRole) new DiscordRoleWrapper(r)).ToList().AsReadOnly(), e.NicknameAfter);
	}

	internal async Task OnMemberRemoved(DiscordClient _, GuildMemberRemoveEventArgs e)
	{
		await HandleMemberRemoved(new DiscordGuildWrapper(e.Guild), new DiscordMemberWrapper(e.Member));
	}

	internal async Task HandleMemberAdded(IDiscordClient sender, IDiscordGuild guild, IDiscordMember member)
	{
		await ExecuteIfAllowedAsync(guild, async () =>
		{
			IDiscordRole noobRole = guild.GetRole(Constants.NOOB_ROLE);

			if (noobRole == null)
			{
				await _errorHandler.HandleErrorAsync("Could not grant new member [" + member.DisplayName + " (" + member.Username + "#" + member.Discriminator + ")] the Noob role.");
				return;
			}

			await member.GrantRoleAsync(noobRole);

			IDiscordChannel welkomChannel = await _channelService.GetWelkomChannel();

			if (welkomChannel == null)
			{
				return;
			}

			IDiscordChannel regelsChannel = await _channelService.GetRegelsChannel();

			welkomChannel.SendMessageAsync(member.Mention + " welkom op de NLBE discord server. Beantwoord eerst de vraag en lees daarna de " + (regelsChannel != null ? regelsChannel.Mention : "#regels") + " aub.").Wait();

			IDiscordUser user = await sender.GetUserAsync(member.Id);

			if (user == null)
			{
				return;
			}

			IReadOnlyList<IWGAccount> searchResults = [];
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
				searchResults = await _wgAccountService.SearchByName(SearchAccuracy.EXACT, ign, _options.WarGamingAppId, false, true, false);

				if (searchResults != null && searchResults.Count > 0)
				{
					resultFound = true;
					foreach (IWGAccount tempAccount in searchResults)
					{
						string tempClanName = string.Empty;
						if (tempAccount.Clan != null)
						{
							tempClanName = tempAccount.Clan.Tag;
						}

						try
						{
							sbDescription.AppendLine(++counter + ". " + tempAccount.Nickname + " " + (tempClanName.Length > 0 ? '`' + tempClanName + '`' : string.Empty));
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

			IWGAccount account = searchResults[selectedAccount];

			string clanName = string.Empty;
			if (account.Clan != null && account.Clan.Tag != null)
			{
				if (account.Clan.Id.Equals(Constants.NLBE_CLAN_ID) || account.Clan.Id.Equals(Constants.NLBE2_CLAN_ID))
				{
					await member.SendMessageAsync("Indien je echt van **" + account.Clan.Tag + "** bent dan moet je even vragen of iemand jouw de **" + account.Clan.Tag + "** rol wilt geven.");
				}
				else
				{
					clanName = account.Clan.Tag;
				}
			}

			_userService.ChangeMemberNickname(member, "[" + clanName + "] " + account.Nickname).Wait();
			await member.SendMessageAsync("We zijn er bijna. Als je nog even de regels wilt lezen in **#regels** dan zijn we klaar.");

			IDiscordRole rulesNotReadRole = guild.GetRole(Constants.MOET_REGELS_NOG_LEZEN_ROLE);

			if (rulesNotReadRole != null)
			{
				await member.RevokeRoleAsync(noobRole);
				await member.GrantRoleAsync(rulesNotReadRole);
			}

			IReadOnlyCollection<IDiscordMember> allMembers = await guild.GetAllMembersAsync();
			bool atLeastOneOtherPlayerWithNoobRole = false;

			foreach (var _ in from IDiscordMember m in allMembers
							  where m.Roles.Contains(noobRole)
							  select new
							  {
							  })
			{
				atLeastOneOtherPlayerWithNoobRole = true;
			}

			if (atLeastOneOtherPlayerWithNoobRole)
			{
				await _channelService.CleanWelkomChannel(member.Id);
			}
			else
			{
				await _channelService.CleanWelkomChannel();
			}
		});
	}

	internal async Task HandleMemberUpdated(IDiscordGuild guild, IDiscordMember member, IReadOnlyList<IDiscordRole> rolesAfter, string nicknameAfter)
	{
		await ExecuteIfAllowedAsync(guild, async () =>
		{
			IEnumerable<IDiscordRole> roles = member.Roles;
			bool isNoob = roles.Any(role => role.Id.Equals(Constants.NOOB_ROLE));
			bool hasRoles = roles.Any();

			// TODO: why do we check rolesAfter and nicknameAfter?
			if (isNoob || !hasRoles || rolesAfter.Count == 0 || string.IsNullOrEmpty(nicknameAfter))
			{
				return;
			}

			string editedName = _userService.UpdateName(member, member.DisplayName);

			if (!string.IsNullOrEmpty(editedName) && !editedName.Equals(member.DisplayName, StringComparison.Ordinal))
			{
				await _userService.ChangeMemberNickname(member, editedName);
			}
		});
	}

	internal async Task HandleMemberRemoved(IDiscordGuild guild, IDiscordMember member)
	{
		await ExecuteIfAllowedAsync(guild, async () =>
		{

			IDiscordChannel oudLedenChannel = await _channelService.GetOudLedenChannel();

			if (oudLedenChannel == null)
			{
				return;
			}

			IReadOnlyDictionary<ulong, IDiscordRole> serverRoles = guild.Roles;

			if (serverRoles == null || serverRoles.Count <= 0)
			{
				return;
			}

			IEnumerable<IDiscordRole> memberRoles = member.Roles;
			StringBuilder sbRoles = new();
			bool firstRole = true;

			foreach (IDiscordRole role in memberRoles)
			{
				foreach (var _ in from KeyValuePair<ulong, IDiscordRole> serverRole in serverRoles
								  where serverRole.Key.Equals(role.Id)
								  select new
								  {
								  })
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

			List<DEF> defList = [];
			DEF newDef1 = new()
			{
				Inline = true,
				Name = "Bijnaam:",
				Value = member.DisplayName
			};
			defList.Add(newDef1);
			DEF newDef2 = new()
			{
				Inline = true,
				Name = "Gebruiker:",
				Value = member.Username + "#" + member.Discriminator
			};
			defList.Add(newDef2);
			DEF newDef3 = new()
			{
				Inline = true,
				Name = "GebruikersID:",
				Value = member.Id.ToString()
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

			EmbedOptions embedOptions = new()
			{
				Title = member.Username + " heeft de server verlaten",
				Fields = defList,
			};

			await _messageService.CreateEmbed(oudLedenChannel, embedOptions);
		});
	}

	private async Task ExecuteIfAllowedAsync(IDiscordGuild guild, Func<Task> action)
	{
		if (_botState.IgnoreEvents || guild.Id != _options.ServerId)
		{
			return;
		}

		await action();
	}
}
