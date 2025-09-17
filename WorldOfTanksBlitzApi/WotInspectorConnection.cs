namespace WorldOfTanksBlitzApi;

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Interfaces;

public class WotInspectorConnection(HttpClient client,
									ILogger<WotInspectorConnection> _logger,
									string baseUri) : IWotInspectorConnection
{
	private readonly HttpClient _httpClient = client ?? throw new ArgumentNullException(nameof(client));
	private readonly ILogger<WotInspectorConnection> _logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
	private readonly string _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));

	public async Task<string> UploadReplayAsync(string relativeUrl, string fileName, byte[] fileContent, string? title, long? accountId)
	{
		string url = _baseUri.TrimEnd(Path.AltDirectorySeparatorChar) + Path.AltDirectorySeparatorChar + relativeUrl.TrimStart(Path.AltDirectorySeparatorChar);

		string base64Data = Convert.ToBase64String(fileContent);

		MultipartFormDataContent form = new()
		{
			{ new StringContent(fileName), "filename" },
			{ new StringContent(base64Data), "file" }
		};

		if (!string.IsNullOrWhiteSpace(title))
		{
			form.Add(new StringContent(title), "title");
		}

		if (accountId.HasValue)
		{
			form.Add(new StringContent(accountId.Value.ToString()), "loaded_by");
		}

		_logger.LogDebug("Uploading replay to WotInspecor at {Url} with filename {FileName} for account {AccountId}", url, fileName, accountId.HasValue ? accountId.Value.ToString() : "N/A");

		HttpResponseMessage response = await _httpClient.PostAsync(url, form);

		return !response.IsSuccessStatusCode ?
				throw new HttpRequestException($"Upload failed: {(int) response.StatusCode} {response.ReasonPhrase}") :
				await response.Content.ReadAsStringAsync();
	}
}
