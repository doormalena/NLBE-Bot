namespace NLBE_Bot.Helpers;

using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

internal class ApiRequester(HttpClient client) : IApiRequester
{
	private readonly HttpClient _client = client ?? throw new ArgumentNullException(nameof(client));

	public async Task<string> GetRequest(string url, Dictionary<string, string>? parameters = null)
	{
		// Set the API key header
		if (parameters != null)
		{
			foreach (KeyValuePair<string, string> parameter in parameters)
			{
				_client.DefaultRequestHeaders.Add(parameter.Key, parameter.Value);
			}
		}

		HttpResponseMessage response = await _client.GetAsync(url);
		return await response.Content.ReadAsStringAsync();
	}
}
