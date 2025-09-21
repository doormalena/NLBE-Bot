namespace WorldOfTanksBlitzApi;

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Exceptions;
using WorldOfTanksBlitzApi.Interfaces;

public class WotInspectorConnection(HttpClient client,
									ILogger<WotInspectorConnection> _logger,
									string baseUri) : IWotInspectorConnection
{
	private readonly HttpClient _httpClient = client ?? throw new ArgumentNullException(nameof(client));
	private readonly ILogger<WotInspectorConnection> _logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
	private readonly string _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));

	public async Task<string> UploadReplayAsync(string relativeUrl, string fileName, byte[] fileContent, string title)
	{
		string url = _baseUri.TrimEnd(Path.AltDirectorySeparatorChar) + Path.AltDirectorySeparatorChar + relativeUrl.TrimStart(Path.AltDirectorySeparatorChar);

		using MultipartFormDataContent form = new()
		{
			{
				new StringContent(title ?? string.Empty),
				"title"
			},
			{
				new ByteArrayContent(fileContent),
				"upload_file",
				fileName
			}
		};

		_logger.LogDebug("Uploading replay to WotInspector at {Url}", url);

		HttpResponseMessage response = await _httpClient.PostAsync(url, form);

		if ((int) response.StatusCode >= 500)
		{
			throw new InternalServerErrorException();
		}

		string content = await response.Content.ReadAsStringAsync();

		try
		{
			/* Examples:
			 * 200 - { "id": "833246b4e310af8087f9bafd38f539f5", "map_id": 23, ... }
			 * 400 - { "title": [ "This field may not be null." ], "upload_file": [ "The submitted data was not a file. Check the encoding type on the form." ] }
			*/
			if (!response.IsSuccessStatusCode)
			{
				StringBuilder errorMessages = new();
				Dictionary<string, string[]>? errorDoc = JsonSerializer.Deserialize<Dictionary<string, string[]>>(content);

				if (errorDoc != null && errorDoc.Count > 0)
				{
					foreach (KeyValuePair<string, string[]> kvp in errorDoc)
					{
						errorMessages.Append($"\n{kvp.Key}: {string.Join("; ", kvp.Value)}");
					}
				}

				throw new HttpRequestException($"Upload failed: {(int) response.StatusCode} {response.ReasonPhrase}{errorMessages}");
			}

			return content;
		}
		catch (JsonException)
		{
			throw new InvalidOperationException("Failed to parse API response.");
		}
	}
}
