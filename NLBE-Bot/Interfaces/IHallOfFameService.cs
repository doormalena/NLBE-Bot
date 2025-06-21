namespace NLBE_Bot.Interfaces;

using DSharpPlus.Entities;
using FMWOTB.Tools.Replays;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal interface IHallOfFameService
{
	public Task<Tuple<string, DiscordMessage>> Handle(string titel, object discAttach, DiscordChannel channel, string guildName, ulong guildID, string iets, DiscordMember member);

	public Task<Tuple<string, DiscordMessage>> GoHOFDetails(WGBattle replayInfo, DiscordChannel channel, DiscordMember member, string guildName, ulong guildID);

	public Task<bool> CreateOrCleanHOFMessages(DiscordChannel HOFchannel, List<Tuple<int, DiscordMessage>> tiersFound);

	public Task EditHOFMessage(DiscordMessage message, List<Tuple<string, List<TankHof>>> tierHOF);

	public Task<List<Tuple<string, List<TankHof>>>> GetTankHofsPerPlayer(ulong guildID);

	public List<Tuple<string, List<TankHof>>> ConvertHOFMessageToTupleListAsync(DiscordMessage message, int TIER);

	public List<DiscordMessage> GetTierMessages(int tier, IReadOnlyList<DiscordMessage> messages);

	public Task HofAfterUpload(Tuple<string, DiscordMessage> returnedTuple, DiscordMessage uploadMessage);

}
