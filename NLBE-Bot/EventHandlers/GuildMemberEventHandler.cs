namespace NLBE_Bot.EventHandlers;

using DSharpPlus;
using DSharpPlus.EventArgs;
using FMWOTB.Account;
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

internal class GuildMemberEventHandler(IDiscordClient discordClient,
									   IErrorHandler errorHandler,
									   ILogger<GuildMemberEventHandler> logger,
									   IOptions<BotOptions> options,
									   IChannelService channelService,
									   IUserService userService,
									   IMessageService messageService) : IGuildMemberEventHandler
{
	private readonly IDiscordClient _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly ILogger<GuildMemberEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

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

	internal async Task HandleMemberAdded(IDiscordClient sender, IDiscordGuild guild, IDiscordMember member)
	{
		if (_botState.IgnoreEvents)
		{
			return;
		}

		if (guild.Id != _options.ServerId)
		{
			return;
		}

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
			searchResults = await WGAccount.searchByName(SearchAccuracy.EXACT, ign, _options.WarGamingAppId, false, true, false);

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
				await member.SendMessageAsync("Indien je echt van **" + account.clan.tag + "** bent dan moet je even vragen of iemand jouw de **" + account.clan.tag + "** rol wilt geven.");
			}
			else
			{
				clanName = account.clan.tag;
			}
		}

		_userService.ChangeMemberNickname(member, "[" + clanName + "] " + account.nickname).Wait();
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
	}
	private async Task OnMemberUpdated(DiscordClient _, GuildMemberUpdateEventArgs e)
	{
		await HandleMemberUpdated(e);
	}

	internal async Task HandleMemberUpdated(GuildMemberUpdateEventArgs e)
	{
		if (_botState.IgnoreEvents)
		{
			return;
		}

		foreach (KeyValuePair<ulong, IDiscordGuild> guild in _discordClient.Guilds.Where(g => g.Key != _options.ServerId))
		{
			IDiscordMember member = await _userService.GetDiscordMember(guild.Value, e.Member.Id);

			if (member == null)
			{
				continue;
			}

			IEnumerable<IDiscordRole> userRoles = member.Roles;
			bool isNoob = userRoles.Any(role => role.Id.Equals(Constants.NOOB_ROLE));
			bool hasRoles = userRoles.Any();

			if (!isNoob && hasRoles && (e.RolesAfter != null || !string.IsNullOrEmpty(e.NicknameAfter)))
			{
				string editedName = _userService.UpdateName(member, member.DisplayName);
				if (!string.IsNullOrEmpty(editedName) && !editedName.Equals(member.DisplayName, StringComparison.Ordinal))
				{
					await _userService.ChangeMemberNickname(member, editedName);
				}
			}
		}
	}
	internal async Task OnMemberRemoved(DiscordClient _, GuildMemberRemoveEventArgs e)
	{
		await HandleMemberRemoved(new DiscordGuildWrapper(e.Guild), new DiscordMemberWrapper(e.Member));
	}

	internal async Task HandleMemberRemoved(IDiscordGuild guild, IDiscordMember member)
	{
		if (_botState.IgnoreEvents)
		{
			return;
		}

		if (guild.Id == _options.ServerId)
		{
			return;
		}

		IDiscordChannel oudLedenChannel = await _channelService.GetOudLedenChannel();
		if (oudLedenChannel != null)
		{
			IReadOnlyDictionary<ulong, IDiscordRole> serverRoles = null;

			foreach (KeyValuePair<ulong, IDiscordGuild> g in from KeyValuePair<ulong, IDiscordGuild> g in _discordClient.Guilds
															 where g.Value.Id == _options.ServerId
															 select g)
			{
				serverRoles = g.Value.Roles;
			}

			if (serverRoles != null && serverRoles.Count > 0)
			{
				IEnumerable<IDiscordRole> memberRoles = member.Roles;
				StringBuilder sbRoles = new();
				bool firstRole = true;
				foreach (IDiscordRole role in memberRoles)
				{
					foreach (KeyValuePair<ulong, IDiscordRole> serverRole in serverRoles)
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

				EmbedOptions options = new()
				{
					Title = member.Username + " heeft de server verlaten",
					Fields = defList,
				};

				await _messageService.CreateEmbed(oudLedenChannel, options);
			}
		}
	}
}
