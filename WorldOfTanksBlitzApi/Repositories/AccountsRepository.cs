namespace WorldOfTanksBlitzApi.Repositories;

using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

public class AccountsRepository(IWotbConnection connection) : IAccountsRepository
{
	private readonly IWotbConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));

	public async Task<IReadOnlyList<WotbAccountListItem>> SearchByNameAsync(SearchType searchType, string term, int maxResults = 20)
	{
		string jsonText = await SearchByName(term, searchType, maxResults);
		WotbAccountList response = JsonSerializer.Deserialize<WotbAccountList>(jsonText);

		return response == null || response.Data == null ? [] : (IReadOnlyList<WotbAccountListItem>) response.Data;
	}

	public async Task<WotbAccountInfo> GetByIdAsync(long accountId)
	{
		string accountJson = await GetById(accountId);

		// The API returns: { "status": "...", "data": { "account_id": { ...account fields... } } }
		JsonNode rootNode = JsonNode.Parse(accountJson);
		JsonNode dataNode = rootNode?["data"];

		if (dataNode != null)
		{
			JsonNode accountNode = dataNode[accountId.ToString()];

			if (accountNode != null && accountNode.ToJsonString() != "null")
			{
				return JsonSerializer.Deserialize<WotbAccountInfo>(accountNode.ToJsonString());
			}
		}

		return null;
	}

	private async Task<string> SearchByName(string searchTerm, SearchType searchType, int limit)
	{
		const string relativeUrl = "/account/list/";

		MultipartFormDataContent form = [];
		form.Add(new StringContent(searchTerm), "search");
		form.Add(new StringContent(limit.ToString()), "limit");
		form.Add(new StringContent(searchType.ToString().ToLower()), "type");

		return await _connection.PostAsync(relativeUrl, form);
	}

	private async Task<string> GetById(long accountId)
	{
		const string relativeUrl = "/account/info/";

		MultipartFormDataContent form = [];
		form.Add(new StringContent(accountId.ToString()), "account_id");
		form.Add(new StringContent("statistics.rating"), "extra");

		return await _connection.PostAsync(relativeUrl, form);
	}
}
