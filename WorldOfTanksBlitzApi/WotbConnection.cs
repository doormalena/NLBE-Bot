namespace FMWOTB;

using FMWOTB.Exceptions;
using FMWOTB.Interfaces;
using System;
using System.Net.Http;
using System.Threading.Tasks;

public class WotbConnection(HttpClient client, string applicationId, string baseUri) : IWotbConnection
{
	private readonly HttpClient _client = client ?? throw new ArgumentNullException(nameof(client));
	private readonly string _applicationId = applicationId ?? throw new ArgumentNullException(nameof(applicationId));
	private readonly string _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));

	public async Task<string> PostAsync(string relativeUrl, MultipartFormDataContent form)
	{
		if (form == null)
		{
			throw new ArgumentNullException(nameof(form));
		}

		// Always add the application_id to the form
		form.Add(new StringContent(_applicationId), "application_id");

		string url = _baseUri.TrimEnd('/') + "/" + relativeUrl.TrimStart('/');

		HttpResponseMessage response = await _client.PostAsync(url, form);

		if ((int) response.StatusCode >= 500)
		{
			throw new InternalServerErrorException();
		}

		return await response.Content.ReadAsStringAsync();
	}
}
