namespace NLBE_Bot.Interfaces;

using FMWOTB.Tools.Replays;
using System.Threading.Tasks;

internal interface IReplayService
{
	public Task<string> GetDescriptionForReplay(WGBattle battle, int position, string preDescription = "");

	public Task<WGBattle> GetReplayInfo(string title, object attachment, string ign, string url);
}
