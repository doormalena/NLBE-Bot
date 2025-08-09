namespace FMWOTB.Repositories;

using FMWOTB.Exceptions;
using FMWOTB.Interfaces;
using FMWOTB.Models;
using FMWOTB.Tools;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

public class AccountsRepository(IWotbConnection connection) : IAccountsRepository
{
	private readonly IWotbConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));

	/// <summary>
	/// 
	/// loadVehicles:
	/// 0 = false
	/// 1 = true
	/// 2 = in garage only
	/// </summary>
	/// <param name="accuracy"></param>
	/// <param name="term"></param>
	/// <param name="wg_application_key"></param>
	/// <param name="loadClanMembers"></param>
	/// <param name="loadClan"></param>
	/// <param name="loadStatistics"></param>
	/// <param name="loadVehicles"></param>
	/// <returns></returns>
	/// <exception cref="TooManyResultsException"></exception>
	public async Task<IReadOnlyList<PlayerInfo>> SearchByNameAsync(
		SearchAccuracy accuracy,
		string term,
		bool loadClanMembers = false,
		bool loadClan = false,
		bool loadStatistics = false,
		short loadVehicles = 0,
		int maxResults = 20)
	{
		string jsonText = await SearchByName(term);
		JsonSerializerOptions options = new()
		{
			PropertyNameCaseInsensitive = true
		};

		PlayerList response = JsonSerializer.Deserialize<PlayerList>(jsonText, options);
		List<PlayerInfo> accountList = [];
		int counter = 0;

		if (response == null || response.Data == null || response.Data.Count <= 0)
		{
			return accountList;
		}

		foreach (PlayerListItem item in response.Data)
		{
			bool addToList = false;
			switch (accuracy) // TODO: why do this in code, the API already supports this using a param
			{
				case SearchAccuracy.STARTS_WITH:
					if (item.Nickname != null && item.Nickname.StartsWith(term))
					{
						addToList = true;
					}
					break;
				case SearchAccuracy.EXACT_CASE_INSENSITIVE:
					if (item.Nickname != null && item.Nickname.Equals(term, StringComparison.OrdinalIgnoreCase))
					{
						addToList = true;
					}
					break;
				case SearchAccuracy.STARTS_WITH_CASE_INSENSITIVE:
					if (item.Nickname != null && item.Nickname.StartsWith(term, StringComparison.OrdinalIgnoreCase))
					{
						addToList = true;
					}
					break;
				case SearchAccuracy.EXACT:
				default:
					if (item.Nickname == term) // TODO: case sensitivity?
					{
						addToList = true;
					}
					break;
			}

			if (addToList)
			{
				if (counter < maxResults)
				{
					counter++;

					PlayerInfo account = await GetByIdAsync(item.AccountId, loadClanMembers, loadClan, loadStatistics, loadVehicles);

					if (account != null)
					{
						accountList.Add(account);
					}
				}
				else
				{
					throw new TooManyResultsException();
				}
			}
		}
		return accountList;
	}

	public async Task<PlayerInfo> GetByIdAsync(long accountId, bool loadClanMembers = false, bool loadClan = false, bool loadStatistics = false, short loadVehicles = 0)
	{
		string accountJson = await GetById(accountId, string.Empty);

		// The API returns: { "status": "...", "data": { "account_id": { ...account fields... } } }
		JsonNode rootNode = JsonNode.Parse(accountJson);
		JsonNode dataNode = rootNode?["data"];

		if (dataNode != null)
		{
			JsonNode accountNode = dataNode[accountId.ToString()];

			if (accountNode != null && accountNode.ToJsonString() != "null")
			{
				JsonSerializerOptions options = new()
				{
					PropertyNameCaseInsensitive = true
				};

				return JsonSerializer.Deserialize<PlayerInfo>(accountNode.ToJsonString(), options);
			}
		}

		return null;
	}

	private async Task<string> SearchByName(string searchTerm)
	{
		const string relativeUrl = "/account/list/"; // https://api.wotblitz.eu/wotb

		MultipartFormDataContent form = [];
		form.Add(new StringContent(searchTerm), "search");

		return await _connection.PostAsync(relativeUrl, form);
	}

	private async Task<string> GetById(long account_id, string searchTerm)
	{
		const string relativeUrl = "/account/info/";

		MultipartFormDataContent form = [];

		if (account_id > 0)
		{
			form.Add(new StringContent(account_id.ToString()), "account_id");
			form.Add(new StringContent("statistics.rating"), "extra");
		}
		else if (searchTerm.Length > 0)
		{
			form.Add(new StringContent(searchTerm), "fields");
		}

		return await _connection.PostAsync(relativeUrl, form);
	}
}
