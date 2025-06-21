namespace NLBE_Bot.Services;

using DiscordHelper;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using FMWOTB;
using FMWOTB.Clans;
using FMWOTB.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

internal class ClanService(IConfiguration configuration, IMessageService messageService, ILogger<ClanService> logger) : IClanService
{
	private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
	private readonly ILogger<ClanService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	public async Task ShowClanInfo(DiscordChannel channel, WGClan clan)
	{
		List<DEF> deflist = [];
		DEF newDef1 = new()
		{
			Name = "Clannaam",
			Value = clan.name.adaptToDiscordChat(),
			Inline = true
		};
		deflist.Add(newDef1);
		DEF newDef2 = new()
		{
			Name = "Aantal leden",
			Value = clan.members_count.ToString(),
			Inline = true
		};
		deflist.Add(newDef2);
		DEF newDef3 = new()
		{
			Name = "ClanID",
			Value = clan.clan_id.ToString(),
			Inline = true
		};
		deflist.Add(newDef3);
		DEF newDef4 = new()
		{
			Name = "ClanTag",
			Value = clan.tag.adaptToDiscordChat(),
			Inline = true
		};
		deflist.Add(newDef4);
		if (clan.created_at.HasValue)
		{
			DEF newDef5 = new()
			{
				Name = "Gemaakt op"
			};
			string[] splitted = clan.created_at.Value.ConvertToDate().Split(' ');
			newDef5.Value = splitted[0] + " " + splitted[1];
			newDef5.Inline = true;
			deflist.Add(newDef5);
		}
		DEF newDef6 = new()
		{
			Name = "Clan motto",
			Value = clan.motto.adaptDiscordLink().adaptToDiscordChat(),
			Inline = false
		};
		deflist.Add(newDef6);
		DEF newDef7 = new()
		{
			Name = "Clan beschrijving",
			Value = clan.description.adaptDiscordLink().adaptToDiscordChat(),
			Inline = false
		};
		deflist.Add(newDef7);

		EmbedOptions options = new()
		{
			Title = "Info over " + clan.name.adaptToDiscordChat(),
			Fields = deflist,
		};
		await _messageService.CreateEmbed(channel, options);
	}

	public async Task<WGClan> SearchForClan(DiscordChannel channel, DiscordMember member, string guildName, string clan_naam, bool loadMembers, DiscordUser user, Command command)
	{
		try
		{
			IReadOnlyList<WGClan> clans = await WGClan.searchByName(SearchAccuracy.STARTS_WITH_CASE_INSENSITIVE, clan_naam, _configuration["NLBEBOT:WarGamingAppId"], loadMembers);
			int aantalClans = clans.Count;
			List<WGClan> clanList = [];
			foreach (WGClan clan in clans)
			{
				if (clan_naam.ToLower().Equals(clan.tag.ToLower()))
				{
					clanList.Add(clan);
				}
			}

			if (clanList.Count > 1)
			{
				StringBuilder sbFound = new();
				for (int i = 0; i < clanList.Count; i++)
				{
					sbFound.AppendLine(i + 1 + ". `" + clanList[i].tag + "`");
				}
				if (sbFound.Length < 1024)
				{
					int index = await _messageService.WaitForReply(channel, user, clan_naam, clanList.Count);
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
				await _messageService.SendMessage(channel, member, guildName, "**Clan(" + clan_naam + ") is niet gevonden! (In een lijst van " + aantalClans + " clans)**");
			}
		}
		catch (TooManyResultsException ex)
		{
			_logger.LogWarning("({Command}) {Message}", command.Name, ex.Message);
			await _messageService.SendMessage(channel, member, guildName, "**Te veel resultaten waren gevonden, wees specifieker!**");
		}
		return null;
	}
}
