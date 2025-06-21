namespace NLBE_Bot.Helpers;

using Newtonsoft.Json;
using NLBE_Bot.Interfaces;
using System;
using System.Net.Http;
using System.Threading.Tasks;

public class PublicIpAddress(HttpClient client) : IPublicIpAddress
{
#pragma warning disable S1075 // URIs should not be hardcod
	private const string ApiUrl = "https://api.ipify.org?format=json";
#pragma warning restore S1075 // URIs should not be hardcoded

	private readonly HttpClient _client = client ?? throw new ArgumentNullException(nameof(client));

	public async Task<string> GetPublicIpAddressAsync()
	{
		try
		{
			string response = await _client.GetStringAsync(ApiUrl);
			IpResponse json = JsonConvert.DeserializeObject<IpResponse>(response);
			return json.Ip;
		}
		catch (Exception ex)
		{
			return $"Unable to retrieve IP, cause: {ex}";
		}
	}
	public class IpResponse
	{
		public string Ip
		{
			get; set;
		}
	}
}
