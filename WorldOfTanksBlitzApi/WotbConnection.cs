namespace WorldOfTanksBlitzApi;

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Exceptions;
using WorldOfTanksBlitzApi.Interfaces;

public class WotbConnection(HttpClient client,
							ILogger<WotbConnection> _logger,
							string baseUri,
							string applicationId,
							int maxRetries = 5) : IWotbConnection
{
	private readonly HttpClient _client = client ?? throw new ArgumentNullException(nameof(client));
	private readonly ILogger<WotbConnection> _logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
	private readonly string _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
	private readonly string _applicationId = applicationId ?? throw new ArgumentNullException(nameof(applicationId));
	private readonly int _maxRetries = maxRetries;

	private const double BaseDelaySeconds = 0.5;
	private readonly Random _random = new();

	public async Task<string> PostAsync(string relativeUrl, MultipartFormDataContent form)
	{
		ArgumentNullException.ThrowIfNull(form);

		form.Add(new StringContent(_applicationId), "application_id"); // Always add the application_id to the form

		string url = _baseUri.TrimEnd(Path.AltDirectorySeparatorChar) + Path.AltDirectorySeparatorChar + relativeUrl.TrimStart(Path.AltDirectorySeparatorChar);

		for (int attempt = 0; attempt < _maxRetries; attempt++)
		{
			HttpResponseMessage response = await _client.PostAsync(url, form);

			if ((int) response.StatusCode >= 500)
			{
				throw new InternalServerErrorException();
			}

			string content = await response.Content.ReadAsStringAsync();

			try
			{
				/* Examples:
				 * {"status":"ok","meta":{"count":0},"data":[]}
				 * {"status":"error","error":{"field":"application_id","message":"INVALID_APPLICATION_ID","code":407,"value":"your_application_id_here"}}
				 * {"status":"error","error":{"code":407,"message":"REQUEST_LIMIT_EXCEEDED","field":"application_id","value":null}}
				*/

				using JsonDocument doc = JsonDocument.Parse(content);
				JsonElement root = doc.RootElement;
				bool isStatusError = root.TryGetProperty("status", out JsonElement statusElement) && statusElement.GetString() == "error";

				if (isStatusError && !await HandleApiError(attempt, root))
				{
					continue;
				}

				return content;
			}
			catch (JsonException)
			{
				throw new InvalidOperationException("Failed to parse API response.");
			}
		}

		throw new TimeoutException("Max retries exceeded due to rate limiting.");
	}

	private async Task<bool> HandleApiError(int attempt, JsonElement root)
	{
		JsonElement error = root.GetProperty("error");
		int errorCode = error.GetProperty("code").GetInt32();
		string message = error.GetProperty("message").GetString();

		if (errorCode == 407)
		{
			if (message is "INVALID_APPLICATION_ID" or "INVALID_IP_ADDRESS" or "APPLICATION_IS_BLOCKED")
			{
				throw new UnauthorizedAccessException(message);
			}

			if (message == "REQUEST_LIMIT_EXCEEDED")
			{
				await RateLimitReachedBackoff(attempt);
				return false;
			}
		}
		else
		{
			throw new InvalidOperationException($"API error {errorCode}: {message}");
		}

		return true;
	}

	private async Task RateLimitReachedBackoff(int attempt)
	{
		double baseDelay = BaseDelaySeconds * Math.Pow(2, attempt);
		double jitter = _random.NextDouble() * baseDelay;
		_logger.LogDebug("Rate limit exceeded. Retrying in {Delay:F2} seconds...", jitter);
		await Task.Delay(TimeSpan.FromSeconds(jitter));
	}
}
