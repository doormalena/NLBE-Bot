namespace NLBE_Bot.Interfaces;

using FMWOTB.Account;
using FMWOTB.Clans;

internal interface IWGAccount
{
	public WGAccount Inner
	{
		get;
	}

	public string Nickname
	{
		get;
	}
	public IWGClan Clan
	{
		get;
	}
}
internal interface IWGClan
{
	public WGClan Inner
	{
		get;
	}
	public int Id
	{
		get;
	}

	public string Tag
	{
		get;
	}
}

