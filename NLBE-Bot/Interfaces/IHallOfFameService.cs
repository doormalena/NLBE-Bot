namespace NLBE_Bot.Interfaces;

using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Tools.Replays;

internal interface IHallOfFameService
{
	public Task<Tuple<string, IDiscordMessage?>> Handle(string titel, object discAttach, IDiscordChannel channel, IDiscordGuild guild, string iets, IDiscordMember member);

	public Task<bool> CreateOrCleanHOFMessages(IDiscordChannel HOFchannel, List<Tuple<int, IDiscordMessage>> tiersFound);

	public Task EditHOFMessage(IDiscordMessage message, List<Tuple<string, List<TankHof>>> tierHOF);

	public Task<List<Tuple<string, List<TankHof>>>> GetTankHofsPerPlayer(IDiscordGuild guild);

	public List<Tuple<string, List<TankHof>>> ConvertHOFMessageToTupleListAsync(IDiscordMessage message, int TIER);

	public List<IDiscordMessage> GetTierMessages(int tier, IReadOnlyList<IDiscordMessage> messages);

	public Task HofAfterUpload(Tuple<string, IDiscordMessage> returnedTuple, IDiscordMessage uploadMessage);

}
