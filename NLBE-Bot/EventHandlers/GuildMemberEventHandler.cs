namespace NLBE_Bot.EventHandlers;

using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot;
using NLBE_Bot.Configuration;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
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

	private IBotState? _botState;

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
		await HandleMemberUpdated(new DiscordGuildWrapper(e.Guild),
								  new DiscordMemberWrapper(e.Member),
								  e.RolesAfter.Select(r => (IDiscordRole) new DiscordRoleWrapper(r)).ToList().AsReadOnly(),
								  e.NicknameAfter);
	}

	[ExcludeFromCodeCoverage(Justification = "Not testable due to DSharpPlus limitations.")]
	private async Task OnMemberRemoved(DiscordClient _, GuildMemberRemoveEventArgs e)
	{
		await HandleMemberRemoved(new DiscordGuildWrapper(e.Guild), new DiscordMemberWrapper(e.Member));
	}

	internal async Task HandleMemberAdded(IDiscordClient sender, IDiscordGuild guild, IDiscordMember member)
	{
		await ExecuteIfAllowedAsync(guild, async () =>
		{
			if (Guard.ReturnIfNull(guild.GetChannel(_options.ChannelIds.Welcome), _logger, "Welcome channel", out IDiscordChannel welcomeChannel) ||
				Guard.ReturnIfNull(guild.GetChannel(_options.ChannelIds.Rules), _logger, "Rules channel", out IDiscordChannel rulesChannel) ||
				Guard.ReturnIfNull(guild.GetRole(_options.RoleIds.Noob), _logger, "Noob role", out IDiscordRole noobRole) ||
				Guard.ReturnIfNull(guild.GetRole(_options.RoleIds.MustReadRules), _logger, "MustReadRules role", out IDiscordRole rulesNotReadRole) ||
				Guard.ReturnIfNull(await sender.GetUserAsync(member.Id), _logger, "User", out IDiscordUser user))
			{
				return;
			}

			await member.GrantRoleAsync(noobRole);
			await welcomeChannel.SendMessageAsync($"{member.Mention} welkom op de NLBE Discord-server! 👋\nBeantwoord eerst de vraag over je WoTB-spelersnaam en neem daarna even de tijd om de regels in {rulesChannel.Mention} goed door te lezen. Veel plezier!");

			IReadOnlyList<WotbAccountListItem> searchResults = await FindMatchingAccountsAsync(welcomeChannel, user, guild);
			WotbAccountListItem selectedAccount = await SelectAccountAsync(searchResults, welcomeChannel, user);
			await ProcessAccountInfoAsync(selectedAccount, member);

			await member.SendMessageAsync("We zijn er bijna!\nNeem nog even een moment om de regels in **#regels** door te nemen, dan ben je helemaal klaar ✅.");
			await member.RevokeRoleAsync(noobRole);
			await member.GrantRoleAsync(rulesNotReadRole);

			// Note: we do not remove the MustReadRules role here. When the user reacts on the rules message (see MessageEventHandler.HandleMessageReactionAdded):
			// - The MustReadRules role will be removed.
			// - The member will get the Members role.
			// - The username will be updated (if needed).
			// - The user will be welcomed in the general channel.

			await _channelService.CleanWelkomChannelAsync(member.Id);
		});
	}

	internal async Task HandleMemberUpdated(IDiscordGuild guild, IDiscordMember member, IReadOnlyList<IDiscordRole> rolesAfter, string nicknameAfter)
	{
		await ExecuteIfAllowedAsync(guild, async () =>
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
		});
	}

	internal async Task HandleMemberRemoved(IDiscordGuild guild, IDiscordMember member)
	{
		await ExecuteIfAllowedAsync(guild, async () =>
		{
			if (Guard.ReturnIfNull(guild.GetChannel(_options.ChannelIds.OldMembers), _logger, "Old Members channel", out IDiscordChannel oudLedenChannel))
			{
				return;
			}

			IReadOnlyDictionary<ulong, IDiscordRole> serverRoles = guild.Roles;
			IEnumerable<IDiscordRole> roles = member.Roles;

			if (roles.Any(role => role.Id.Equals(_options.RoleIds.Noob)))
			{
				IDiscordRole noobRole = guild.GetRole(_options.RoleIds.Noob);
				await _channelService.CleanWelkomChannelAsync(member.Id);
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

	private async Task<IReadOnlyList<WotbAccountListItem>> FindMatchingAccountsAsync(IDiscordChannel channel, IDiscordUser user, IDiscordGuild guild)
	{
		bool firstAttempt = true;

		while (true) // TODO: add a retry limit to prevent a never ending loop.
		{
			string question;
			if (firstAttempt)
			{
				question = $"{user.Mention} Wat is je WoTB-spelersnaam?";
				firstAttempt = false;
			}
			else
			{
				question = $"**We konden je opgegeven WoTB-spelersnaam niet vinden. Kun je het nog eens proberen?** \n{user.Mention} Wat is je WoTB-spelersnaam?";
			}

			string playerName = await _messageService.AskQuestion(channel, user, guild, question);
			IReadOnlyList<WotbAccountListItem> searchResults = await _accountRepository.SearchByNameAsync(SearchType.StartsWith, playerName);

			if (searchResults.Count > 0)
			{
				return searchResults;
			}
		}
	}
	private async Task<WotbAccountListItem> SelectAccountAsync(IReadOnlyList<WotbAccountListItem> accounts, IDiscordChannel channel, IDiscordUser user)
	{
		if (accounts.Count == 1)
		{
			return accounts[0];
		}

		StringBuilder sb = new();
		int counter = 0;

		for (int i = 0; i < accounts.Count; i++)
		{
			WotbAccountListItem account = accounts[i];
			WotbAccountInfo? accountInfo = await _accountRepository.GetByIdAsync(account.AccountId);

			if (accountInfo == null)
			{
				continue;
			}

			WotbAccountClanInfo? clanInfo = await _clanRepository.GetAccountClanInfoAsync(accountInfo.AccountId);
			string clanTag = (clanInfo != null && clanInfo.Clan != null) ? clanInfo.Clan.Tag : string.Empty;

			sb.AppendLine((++counter) + ". " + accountInfo.Nickname + (string.IsNullOrEmpty(clanTag) ? "" : " `" + clanTag + "`"));
		}

		int selected = -1;
		while (selected == -1) // TODO: add a retry limit to prevent a never ending loop.
		{
			selected = await _messageService.WaitForReply(channel, user, sb.ToString(), counter);
		}

		return accounts[selected];
	}

	private async Task ProcessAccountInfoAsync(WotbAccountListItem account, IDiscordMember member)
	{
		string clanTag = string.Empty;
		WotbAccountClanInfo? clanInfo = await _clanRepository.GetAccountClanInfoAsync(account.AccountId);

		if (clanInfo != null)
		{
			if (clanInfo.ClanId.Equals(Constants.NLBE_CLAN_ID) || clanInfo.ClanId.Equals(Constants.NLBE2_CLAN_ID)) // TODO: move to configuration
			{
				await member.SendMessageAsync("Indien je echt van **" + clanInfo.Clan.Tag + "** bent dan moet je even vragen of iemand jouw de **" + clanInfo.Clan.Tag + "** rol wilt geven."); // TODO: why is this manual, and not automated.
			}
			else if (clanInfo.Clan != null && clanInfo.Clan.Tag != null)
			{
				clanTag = clanInfo.Clan.Tag;
			}
		}

		await _userService.ChangeMemberNickname(member, "[" + clanTag + "] " + account.Nickname);
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

	private async Task ExecuteIfAllowedAsync(IDiscordGuild guild, Func<Task> action)
	{
		if (_botState!.IgnoreEvents || guild.Id != _options.ServerId)
		{
			return;
		}

		await action();
	}
}
