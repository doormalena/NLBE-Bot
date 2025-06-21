namespace NLBE_Bot.Models;

using System;
using System.Collections.Generic;

public class Tier
{
	public string Organisator { get; set; } = string.Empty;
	private readonly List<Tuple<ulong, string>> _deelnemers = [];
	public string TierNummer { get; set; } = string.Empty;
	public string Datum { get; set; } = string.Empty;
	public int Index { get; set; } = 0;
	public List<string> Uniekelingen { get; set; } = [];
	private readonly bool editedWithRedundance = false;
	public List<Tuple<ulong, string>> Deelnemers => _deelnemers;

	public void AddDeelnemer(string naam, ulong id)
	{
		_deelnemers.Add(new Tuple<ulong, string>(id, naam));
	}

	public bool RemoveDeelnemer(ulong id)
	{
		for (int i = 0; i < _deelnemers.Count; i++)
		{
			if (_deelnemers[i].Item1.Equals(id))
			{
				_deelnemers.RemoveAt(i);
				return true;
			}
		}
		return false;
	}

	public bool IsEditedWithRedundance()
	{
		return editedWithRedundance;
	}
}
