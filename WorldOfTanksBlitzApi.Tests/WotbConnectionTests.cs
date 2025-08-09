namespace WorldOfTanksBlitzApi.Tests;

using FMWOTB;
using FMWOTB.Exceptions;
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

	[TestMethod]
	public async Task PostAsync_ReturnsContent_OnSuccess()
	{
		MockHttpMessageHandler mockHttp = new();
		string expectedUrl = BaseUri.TrimEnd('/') + "/" + RelativeUrl.TrimStart('/');

		mockHttp.When(HttpMethod.Post, expectedUrl)
			.Respond("application/json", ExpectedContent);

		HttpClient httpClient = mockHttp.ToHttpClient();

		WotbConnection connection = new(httpClient, ApplicationId, BaseUri);

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		string result = await connection.PostAsync(RelativeUrl, form);

		Assert.AreEqual(ExpectedContent, result);
	}

	[TestMethod]
	public async Task PostAsync_ThrowsInternalServerErrorException_OnServerError()
	{
		MockHttpMessageHandler mockHttp = new();
		string expectedUrl = BaseUri.TrimEnd('/') + "/" + RelativeUrl.TrimStart('/');

		mockHttp.When(HttpMethod.Post, expectedUrl)
			.Respond(HttpStatusCode.InternalServerError);

		HttpClient httpClient = mockHttp.ToHttpClient();

		WotbConnection connection = new(httpClient, ApplicationId, BaseUri);

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		await Assert.ThrowsExceptionAsync<InternalServerErrorException>(async () =>
		{
			await connection.PostAsync(RelativeUrl, form);
		});
	}

	[TestMethod]
	public async Task PostAsync_AppendsApplicationIdToForm()
	{
		string? foundAppId = null;

		MockHttpMessageHandler mockHttp = new();
		string expectedUrl = BaseUri.TrimEnd('/') + "/" + RelativeUrl.TrimStart('/');

		mockHttp.When(HttpMethod.Post, expectedUrl)
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

		HttpClient httpClient = mockHttp.ToHttpClient();

		WotbConnection connection = new(httpClient, ApplicationId, BaseUri);

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		string result = await connection.PostAsync(RelativeUrl, form);

		Assert.AreEqual(ApplicationId, foundAppId, "application_id was not found in the form data.");
	}

	[TestMethod]
	public async Task PostAsync_ThrowsArgumentNullException_WhenFormIsNull()
	{
		MockHttpMessageHandler mockHttp = new();
		HttpClient httpClient = mockHttp.ToHttpClient();

		WotbConnection connection = new(httpClient, ApplicationId, BaseUri);

		await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
		{
			await connection.PostAsync(RelativeUrl, null);
		});
	}
}
