namespace NLBE_Bot.Models;

using FMWOTB.Account;
using FMWOTB.Clans;
using NLBE_Bot.Interfaces;
using System;

internal class WGAccountWrapper(WGAccount account) : IWGAccount
{
	private readonly WGAccount _account = account ?? throw new ArgumentNullException(nameof(account));

	public WGAccount Inner => _account;

	public string Nickname => _account.nickname;

	public IWGClan Clan => _account.clan != null ? new WgClanWrapper(_account.clan) : null;
}

internal class WgClanWrapper(WGClan clan) : IWGClan
{
	private readonly WGClan _clan = clan ?? throw new ArgumentNullException(nameof(clan));

	public WGClan Inner => _clan;

	public int Id => _clan.clan_id;

	public string Tag => _clan.tag;
}
