namespace NLBE_Bot.Helpers;

using System.Collections.Generic;
using System.Net.Http;
public static class ApiRequester
{
	public static string GetRequest(string url, Dictionary<string, string> parameters = null)
	{
		using HttpClient client = new();

		// Set the API key header
		if (parameters != null)
		{
			foreach (KeyValuePair<string, string> parameter in parameters)
			{
				client.DefaultRequestHeaders.Add(parameter.Key, parameter.Value);
			}
		}
		HttpResponseMessage response = client.GetAsync(url).Result;
		return response.Content.ReadAsStringAsync().Result;
	}
}
