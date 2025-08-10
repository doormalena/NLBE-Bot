namespace NLBE_Bot.Services;

using DiscordHelper;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

internal class ClanService(IMessageService messageService,
						   IClansRepository clanRepository,
						   ILogger<ClanService> logger) : IClanService
{
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
	private readonly IClansRepository _clanRepository = clanRepository ?? throw new ArgumentNullException(nameof(clanRepository));
	private readonly ILogger<ClanService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async Task ShowClanInfo(IDiscordChannel channel, WotbClanInfo clan)
	{
		List<DEF> deflist = [];
		DEF newDef1 = new()
		{
			Name = "Clannaam",
			Value = clan.Name.AdaptToDiscordChat(),
			Inline = true
		};
		deflist.Add(newDef1);
		DEF newDef2 = new()
		{
			Name = "Aantal leden",
			Value = clan.MembersCount.ToString(),
			Inline = true
		};
		deflist.Add(newDef2);
		DEF newDef3 = new()
		{
			Name = "ClanID",
			Value = clan.ClanId.ToString(),
			Inline = true
		};
		deflist.Add(newDef3);
		DEF newDef4 = new()
		{
			Name = "ClanTag",
			Value = clan.Tag.AdaptToDiscordChat(),
			Inline = true
		};
		deflist.Add(newDef4);
		if (clan.CreatedAt.HasValue)
		{
			DEF newDef5 = new()
			{
				Name = "Gemaakt op"
			};
			string[] splitted = clan.CreatedAt.Value.ConvertToDate().Split(' ');
			newDef5.Value = splitted[0] + " " + splitted[1];
			newDef5.Inline = true;
			deflist.Add(newDef5);
		}
		DEF newDef6 = new()
		{
			Name = "Clan motto",
			Value = clan.Motto.AdaptDiscordLink().AdaptToDiscordChat(),
			Inline = false
		};
		deflist.Add(newDef6);
		DEF newDef7 = new()
		{
			Name = "Clan beschrijving",
			Value = clan.Description.AdaptDiscordLink().AdaptToDiscordChat(),
			Inline = false
		};
		deflist.Add(newDef7);

		EmbedOptions embedOptions = new()
		{
			Title = "Info over " + clan.Name.AdaptToDiscordChat(),
			Fields = deflist,
		};
		await _messageService.CreateEmbed(channel, embedOptions);
	}

	public async Task<WotbClanListItem> SearchForClan(IDiscordChannel channel, IDiscordMember member, string guildName, string name, bool loadMembers, IDiscordUser user, IDiscordCommand command)
	{
		IReadOnlyList<WotbClanListItem> clans = await _clanRepository.SearchByNameAsync(SearchType.StartsWith, name, loadMembers);
		int aantalClans = clans.Count;
		List<WotbClanListItem> clanList = [];
		clanList.AddRange(from WotbClanListItem clan in clans
						  where name.ToLower().Equals(clan.Tag.ToLower())
						  select clan);

		if (clanList.Count > 1)
		{
			StringBuilder sbFound = new();
			for (int i = 0; i < clanList.Count; i++)
			{
				sbFound.AppendLine(i + 1 + ". `" + clanList[i].Tag + "`");
			}
			if (sbFound.Length < 1024)
			{
				int index = await _messageService.WaitForReply(channel, user, name, clanList.Count);
				if (index >= 0)
				{
					return clanList[index];
				}
			}
			else
			{
				await _messageService.SayBeMoreSpecific(channel);
			}
		}
		else if (clanList.Count == 1)
		{
			return clanList[0];
		}
		else if (clanList.Count == 0)
		{
			await _messageService.SendMessage(channel, member, guildName, "**Clan(" + name + ") is niet gevonden! (In een lijst van " + aantalClans + " clans)**");
		}

		return null;
	}
}
