namespace WorldOfTanksBlitzApi.Repositories;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

public class ClansRepository(IWotbConnection connection) : IClansRepository
{
	private readonly IWotbConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));

	public async Task<IReadOnlyList<WotbClanListItem>> SearchByNameAsync(SearchType searchType, string term, bool loadMembers = false, int maxResults = 20)
	{
		string jsonText = await SearchByName(term, searchType, maxResults);
		WotbClanList response = JsonSerializer.Deserialize<WotbClanList>(jsonText);

		return response == null || response.Data == null ? [] : (IReadOnlyList<WotbClanListItem>) response.Data;
	}

	public async Task<WotbClanInfo> GetByIdAsync(long clanId, bool loadMembers = false)
	{
		string clanJson = await GetById(clanId, loadMembers);

		// The API returns: { "status": "...", "data": { "clan_id": { ...clan fields... } } }
		JsonNode rootNode = JsonNode.Parse(clanJson);
		JsonNode dataNode = rootNode?["data"];

		if (dataNode != null)
		{
			JsonNode clanNode = dataNode[clanId.ToString()];

			if (clanNode != null && clanNode.ToJsonString() != "null")
			{
				return JsonSerializer.Deserialize<WotbClanInfo>(clanNode.ToJsonString());
			}
		}

		return null;
	}

	public async Task<WotbAccountClanInfo> GetAccountClanInfoAsync(long accountId)
	{
		string accountClanJson = await GetAccountClanInfo(accountId);

		// The API returns: { "status": "...", "data": { "account_id": { ...account-clan fields... } } }
		JsonNode rootNode = JsonNode.Parse(accountClanJson);
		JsonNode dataNode = rootNode?["data"];

		if (dataNode != null)
		{
			JsonNode accountClanNode = dataNode[accountId.ToString()];

			if (accountClanNode != null && accountClanNode.ToJsonString() != "null")
			{
				return JsonSerializer.Deserialize<WotbAccountClanInfo>(accountClanNode.ToJsonString());
			}
		}

		return null;
	}

	private async Task<string> GetAccountClanInfo(long accountId)
	{
		const string relativeUrl = "/clans/accountinfo/";

		MultipartFormDataContent form = [];
		form.Add(new StringContent(accountId.ToString()), "account_id");
		form.Add(new StringContent("clan"), "extra");

		return await _connection.PostAsync(relativeUrl, form);
	}

	private async Task<string> SearchByName(string searchTerm, SearchType searchType, int limit)
	{
		const string relativeUrl = "/clans/list/";

		MultipartFormDataContent form = [];
		form.Add(new StringContent(searchTerm), "search");
		form.Add(new StringContent(limit.ToString()), "limit");
		form.Add(new StringContent(searchType.ToString().ToLower()), "type");

		return await _connection.PostAsync(relativeUrl, form);
	}

	private async Task<string> GetById(long clanId, bool loadMembers)
	{
		const string relativeUrl = "/clans/info/";

		MultipartFormDataContent form = [];
		form.Add(new StringContent(clanId.ToString()), "clan_id");

		if (loadMembers)
		{
			form.Add(new StringContent("members"), "extra");
		}

		return await _connection.PostAsync(relativeUrl, form);
	}
}
