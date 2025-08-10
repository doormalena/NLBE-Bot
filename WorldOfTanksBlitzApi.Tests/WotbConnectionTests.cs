namespace WorldOfTanksBlitzApi.Tests;

using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using RichardSzalay.MockHttp;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

[TestClass]
public class WotbConnectionTests
{
	private const string ApplicationId = "test_app_id";
	private const string BaseUri = "https://api.test.com";
	private const string RelativeUrl = "endpoint";
	private const string ExpectedContent = "{ \"status\": \"ok\", \"data\": [ ] }";
	private MockHttpMessageHandler? _mockHttp;
	private HttpClient? _httpClient;

	[TestInitialize]
	public void Setup()
	{
		_mockHttp = new();
		_httpClient = _mockHttp.ToHttpClient();
	}

	[TestMethod]
	public async Task PostAsync_ReturnsContent_OnSuccess()
	{
		// Arrange.
		string expectedUrl = BaseUri.TrimEnd('/') + "/" + RelativeUrl.TrimStart('/');

		_mockHttp!.When(HttpMethod.Post, expectedUrl)
			.Respond("application/json", ExpectedContent);

		WotbConnection connection = new(_httpClient, ApplicationId, BaseUri);

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		// Act.
		string result = await connection.PostAsync(RelativeUrl, form);

		// Assert.
		Assert.AreEqual(ExpectedContent, result);
	}

	[TestMethod]
	public async Task PostAsync_ThrowsInternalServerErrorException_OnServerError()
	{
		// Arrange.
		string expectedUrl = BaseUri.TrimEnd('/') + "/" + RelativeUrl.TrimStart('/');

		_mockHttp!.When(HttpMethod.Post, expectedUrl)
			.Respond(HttpStatusCode.InternalServerError);

		WotbConnection connection = new(_httpClient, ApplicationId, BaseUri);

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		// Act & Assert.
		await Assert.ThrowsExceptionAsync<InternalServerErrorException>(async () =>
		{
			await connection.PostAsync(RelativeUrl, form);
		});
	}

	[TestMethod]
	public async Task PostAsync_AppendsApplicationIdToForm()
	{
		// Arrange.
		string? foundAppId = null;

		string expectedUrl = BaseUri.TrimEnd('/') + "/" + RelativeUrl.TrimStart('/');

		_mockHttp!.When(HttpMethod.Post, expectedUrl)
			.With(message =>
			{
				if (message.Content is MultipartFormDataContent formData)
				{
					foreach (HttpContent content in formData)
					{
						if (content is StringContent stringContent)
						{
							string value = stringContent.ReadAsStringAsync().Result;
							if (value == ApplicationId)
							{
								foundAppId = value;
								return true;
							}
						}
					}
				}
				return false;
			})
			.Respond("application/json", ExpectedContent);

		WotbConnection connection = new(_httpClient, ApplicationId, BaseUri);

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		// Act.
		_ = await connection.PostAsync(RelativeUrl, form);

		// Assert.
		Assert.AreEqual(ApplicationId, foundAppId, "application_id was not found in the form data.");
	}

	[TestMethod]
	public async Task PostAsync_ThrowsArgumentNullException_WhenFormIsNull()
	{
		// Act & Assert.
		WotbConnection connection = new(_httpClient, ApplicationId, BaseUri);

		await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
		{
			await connection.PostAsync(RelativeUrl, null);
		});
	}
}
