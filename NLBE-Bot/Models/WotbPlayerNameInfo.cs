namespace NLBE_Bot.Models;

internal class WotbPlayerNameInfo(string clanTag, string playerName)
{
	public string ClanTag
	{
		get; set;
	} = clanTag;

	public string PlayerName
	{
		get; set;
	} = playerName;

	public bool HasClanTag => !string.IsNullOrEmpty(ClanTag);
}
