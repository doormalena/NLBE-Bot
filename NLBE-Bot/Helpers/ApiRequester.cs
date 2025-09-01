namespace NLBE_Bot.Helpers;

using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;

internal class ApiRequester(HttpClient client) : IApiRequester
{
	private readonly HttpClient _client = client ?? throw new ArgumentNullException(nameof(client));

	public string GetRequest(string url, Dictionary<string, string>? parameters = null)
	{
		// Set the API key header
		if (parameters != null)
		{
			foreach (KeyValuePair<string, string> parameter in parameters)
			{
				_client.DefaultRequestHeaders.Add(parameter.Key, parameter.Value);
			}
		}

		HttpResponseMessage response = _client.GetAsync(url).Result;
		return response.Content.ReadAsStringAsync().Result;
	}
}
