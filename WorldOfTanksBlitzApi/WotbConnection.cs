namespace WorldOfTanksBlitzApi;

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Exceptions;
using WorldOfTanksBlitzApi.Interfaces;

public class WotbConnection(HttpClient client, string applicationId, string baseUri) : IWotbConnection
{
	private readonly HttpClient _client = client ?? throw new ArgumentNullException(nameof(client));
	private readonly string _applicationId = applicationId ?? throw new ArgumentNullException(nameof(applicationId));
	private readonly string _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));

	public async Task<string> PostAsync(string relativeUrl, MultipartFormDataContent form)
	{
		ArgumentNullException.ThrowIfNull(form);

		// Always add the application_id to the form
		form.Add(new StringContent(_applicationId), "application_id");

		string url = _baseUri.TrimEnd(Path.AltDirectorySeparatorChar) + Path.AltDirectorySeparatorChar + relativeUrl.TrimStart(Path.AltDirectorySeparatorChar);

		HttpResponseMessage response = await _client.PostAsync(url, form);

		return (int) response.StatusCode >= 500 ?
				throw new InternalServerErrorException() :
				await response.Content.ReadAsStringAsync();
	}
}
