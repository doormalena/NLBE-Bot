namespace NLBE_Bot.Interfaces;

using WorldOfTanksBlitzApi.Tools.Replays;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal interface IHallOfFameService
{
	public Task<Tuple<string, IDiscordMessage>> Handle(string titel, object discAttach, IDiscordChannel channel, string guildName, ulong guildID, string iets, IDiscordMember member);

	public Task<Tuple<string, IDiscordMessage>> GoHOFDetails(WGBattle replayInfo, IDiscordChannel channel, IDiscordMember member, string guildName, ulong guildID);

	public Task<bool> CreateOrCleanHOFMessages(IDiscordChannel HOFchannel, List<Tuple<int, IDiscordMessage>> tiersFound);

	public Task EditHOFMessage(IDiscordMessage message, List<Tuple<string, List<TankHof>>> tierHOF);

	public Task<List<Tuple<string, List<TankHof>>>> GetTankHofsPerPlayer(ulong guildID);

	public List<Tuple<string, List<TankHof>>> ConvertHOFMessageToTupleListAsync(IDiscordMessage message, int TIER);

	public List<IDiscordMessage> GetTierMessages(int tier, IReadOnlyList<IDiscordMessage> messages);

	public Task HofAfterUpload(Tuple<string, IDiscordMessage> returnedTuple, IDiscordMessage uploadMessage);

}
