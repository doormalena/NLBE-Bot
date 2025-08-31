namespace NLBE_Bot.EventHandlers;

using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

internal class GuildMemberEventHandler(ILogger<GuildMemberEventHandler> logger,
									   IOptions<BotOptions> options,
									   IChannelService channelService,
									   IUserService userService,
									   IMessageService messageService,
									   IAccountsRepository accountRepository,
									   IClansRepository clanRepository) : IGuildMemberEventHandler
{
	private readonly ILogger<GuildMemberEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
	private readonly IAccountsRepository _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
	private readonly IClansRepository _clanRepository = clanRepository ?? throw new ArgumentNullException(nameof(clanRepository));

	private IBotState _botState;

	public void Register(IDiscordClient client, IBotState botState)
	{
		_ = client ?? throw new ArgumentNullException(nameof(client));
		_botState = botState ?? throw new ArgumentNullException(nameof(botState));

		client.GuildMemberAdded += OnMemberAdded;
		client.GuildMemberUpdated += OnMemberUpdated;
		client.GuildMemberRemoved += OnMemberRemoved;
	}

	[ExcludeFromCodeCoverage(Justification = "Not testable due to DSharpPlus limitations.")]
	private async Task OnMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
	{
		await HandleMemberAdded(new DiscordClientWrapper(sender), new DiscordGuildWrapper(e.Guild), new DiscordMemberWrapper(e.Member));
	}

	[ExcludeFromCodeCoverage(Justification = "Not testable due to DSharpPlus limitations.")]
	private async Task OnMemberUpdated(DiscordClient _, GuildMemberUpdateEventArgs e)
	{
		ReadOnlyCollection<IDiscordRole> rolesAfter = e.RolesAfter.Select(r => (IDiscordRole) new DiscordRoleWrapper(r)).ToList().AsReadOnly();
		await HandleMemberUpdated(new DiscordGuildWrapper(e.Guild), new DiscordMemberWrapper(e.Member), rolesAfter, e.NicknameAfter);
	}

	[ExcludeFromCodeCoverage(Justification = "Not testable due to DSharpPlus limitations.")]
	internal async Task OnMemberRemoved(DiscordClient _, GuildMemberRemoveEventArgs e)
	{
		await HandleMemberRemoved(new DiscordGuildWrapper(e.Guild), new DiscordMemberWrapper(e.Member));
	}

	internal async Task HandleMemberAdded(IDiscordClient sender, IDiscordGuild guild, IDiscordMember member)
	{
		await ExecuteIfAllowedAsync(guild, async () =>
		{
			IDiscordChannel welkomChannel = await _channelService.GetWelkomChannel();

			if (welkomChannel == null)
			{
				_logger.LogWarning("Could not find the welcome channel. Cannot process newly added member {MemberName} ({MemberId})", member.DisplayName, member.Id);
				return;
			}

			IDiscordUser user = await sender.GetUserAsync(member.Id);

			if (user == null)
			{
				_logger.LogWarning("Could not find the user. Cannot process newly added member {MemberName} ({MemberId})", member.DisplayName, member.Id);
				return;
			}

			IDiscordRole noobRole = guild.GetRole(_options.RoleIds.Noob);

			if (noobRole == null)
			{
				_logger.LogWarning("Noob role not found. Cannot process newly added member {MemberName} ({MemberId})", member.DisplayName, member.Id);
				return;
			}

			await member.GrantRoleAsync(noobRole);

			IDiscordChannel regelsChannel = await _channelService.GetRegelsChannel();
			await welkomChannel.SendMessageAsync(member.Mention + " welkom op de NLBE discord server. Beantwoord eerst de vraag en lees daarna de " + (regelsChannel != null ? regelsChannel.Mention : "#regels") + " aub.");

			IReadOnlyList<WotbAccountListItem> searchResults = [];
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
				searchResults = await _accountRepository.SearchByNameAsync(SearchType.StartsWith, ign);

				if (searchResults == null || searchResults.Count <= 0)
				{
					continue;
				}

				resultFound = true;
				foreach (WotbAccountListItem tempAccount in searchResults)
				{
					WotbAccountInfo accountInfo = await _accountRepository.GetByIdAsync(searchResults[0].AccountId);

					if (accountInfo == null)
					{
						_logger.LogWarning("Account info not found for account ID {AccountId} while processing member {MemberName} ({MemberId})", searchResults[0].AccountId, member.DisplayName, member.Id);
						continue;
					}

					WotbAccountClanInfo tempAccountClanInfo = await _clanRepository.GetAccountClanInfoAsync(accountInfo.AccountId);
					string tempClanName = tempAccountClanInfo?.Clan.Tag;

					sbDescription.AppendLine(++counter + ". " + accountInfo.Nickname + " " + (!string.IsNullOrEmpty(tempClanName) ? '`' + tempClanName + '`' : string.Empty));
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

			WotbAccountListItem account = searchResults[selectedAccount];
			WotbAccountClanInfo accountClanInfo = await _clanRepository.GetAccountClanInfoAsync(account.AccountId);

			string clanName = string.Empty;

			if (accountClanInfo.Clan != null && accountClanInfo.Clan.Tag != null)
			{
				if (accountClanInfo.ClanId.Equals(Constants.NLBE_CLAN_ID) || accountClanInfo.ClanId.Equals(Constants.NLBE2_CLAN_ID)) // TODO: move to configuration
				{
					await member.SendMessageAsync("Indien je echt van **" + accountClanInfo.Clan.Tag + "** bent dan moet je even vragen of iemand jouw de **" + accountClanInfo.Clan.Tag + "** rol wilt geven.");
				}
				else
				{
					clanName = accountClanInfo.Clan.Tag;
				}
			}

			_userService.ChangeMemberNickname(member, "[" + clanName + "] " + account.Nickname).Wait();
			await member.SendMessageAsync("We zijn er bijna. Als je nog even de regels wilt lezen in **#regels** dan zijn we klaar.");

			IDiscordRole rulesNotReadRole = guild.GetRole(_options.RoleIds.MustReadRules);

			if (rulesNotReadRole != null)
			{
				await member.RevokeRoleAsync(noobRole);
				await member.GrantRoleAsync(rulesNotReadRole);
			}

			await CleanWelcomeChannel(guild, member, noobRole);
		});
	}

	internal async Task HandleMemberUpdated(IDiscordGuild guild, IDiscordMember member, IReadOnlyList<IDiscordRole> rolesAfter, string nicknameAfter)
	{
		await ExecuteIfAllowedAsync(guild, async () =>
		{
			try
			{
				IEnumerable<IDiscordRole> roles = member.Roles;

				// TODO: why do we check rolesAfter and nicknameAfter? Filtering out changes that do not effect the name of the member?
				if (roles.Any(role => role.Id.Equals(_options.RoleIds.Noob)) || !roles.Any() || rolesAfter.Count == 0 || string.IsNullOrEmpty(nicknameAfter))
				{
					return;
				}

				string editedName = _userService.UpdateName(member, member.DisplayName); // TODO: what does this do? Does this update the display name based on the nickname or something else?

				if (!string.IsNullOrEmpty(editedName) && !editedName.Equals(member.DisplayName, StringComparison.Ordinal))
				{
					await _userService.ChangeMemberNickname(member, editedName);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occured while processing the updated member. {DisplayName} ({Id})", member.DisplayName, member.Id);
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
				_logger.LogWarning("Could not find the Oud Leden channel. Cannot log member removal for {MemberName} ({MemberId})", member.DisplayName, member.Id);
				return;
			}

			IReadOnlyDictionary<ulong, IDiscordRole> serverRoles = guild.Roles;

			if (serverRoles == null || serverRoles.Count <= 0)
			{
				_logger.LogWarning("Could not find server roles. Cannot log member removal for {MemberName} ({MemberId})", member.DisplayName, member.Id);
				return;
			}

			IEnumerable<IDiscordRole> roles = member.Roles;

			if (roles.Any(role => role.Id.Equals(_options.RoleIds.Noob)))
			{
				IDiscordRole noobRole = guild.GetRole(_options.RoleIds.Noob);
				await CleanWelcomeChannel(guild, member, noobRole);
			}

			List<DEF> fields = [];

			fields.Add(new()
			{
				Inline = true,
				Name = "Bijnaam:",
				Value = member.DisplayName
			});

			fields.Add(new()
			{
				Inline = true,
				Name = "Gebruiker:",
				Value = member.Username + "#" + member.Discriminator
			});

			fields.Add(new()
			{
				Inline = true,
				Name = "GebruikersID:",
				Value = member.Id.ToString()
			});

			fields.Add(new()
			{
				Inline = true,
				Name = "Rollen:",
				Value = GetCommaSeperatedRoleList(serverRoles, roles)
			});

			EmbedOptions embedOptions = new()
			{
				Title = member.Username + " heeft de server verlaten",
				Fields = fields,
			};

			await _messageService.CreateEmbed(oudLedenChannel, embedOptions);
		});
	}

	private static string GetCommaSeperatedRoleList(IReadOnlyDictionary<ulong, IDiscordRole> serverRoles, IEnumerable<IDiscordRole> memberRoles)
	{
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

		return sbRoles.ToString();
	}

	private async Task CleanWelcomeChannel(IDiscordGuild guild, IDiscordMember member, IDiscordRole noobRole)
	{
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

	private async Task ExecuteIfAllowedAsync(IDiscordGuild guild, Func<Task> action)
	{
		if (_botState.IgnoreEvents || guild.Id != _options.ServerId)
		{
			return;
		}

		await action();
	}
}
