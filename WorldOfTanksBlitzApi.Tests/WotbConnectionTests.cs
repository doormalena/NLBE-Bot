namespace WorldOfTanksBlitzApi.Tests;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using RichardSzalay.MockHttp;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Exceptions;

[TestClass]
public class WotbConnectionTests
{
	private const string ApplicationId = "test_app_id";
	private const string BaseUri = "https://api.test.com";
	private const string RelativeUrl = "endpoint";
	private const string ExpectedContent = "{ \"status\": \"ok\", \"data\": [ ] }";
	private MockHttpMessageHandler? _mockHttp;
	private HttpClient? _httpClient;
	private ILogger<WotbConnection>? _loggerMock;
	private WotbConnection? _connection;

	[TestInitialize]
	public void Setup()
	{
		_mockHttp = new();
		_httpClient = _mockHttp.ToHttpClient();
		_loggerMock = Substitute.For<ILogger<WotbConnection>>();
		_connection = new(_httpClient, _loggerMock, BaseUri, ApplicationId);
	}

	[TestMethod]
	public async Task PostAsync_ReturnsContent_OnSuccess()
	{
		// Arrange.
		string expectedUrl = BaseUri.TrimEnd('/') + "/" + RelativeUrl.TrimStart('/');

		_mockHttp!.When(HttpMethod.Post, expectedUrl).Respond("application/json", ExpectedContent);

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		// Act.
		string result = await _connection!.PostAsync(RelativeUrl, form);

		// Assert.
		Assert.AreEqual(ExpectedContent, result);
	}

	[TestMethod]
	public async Task PostAsync_ThrowsInternalServerErrorException_OnServerError()
	{
		// Arrange.
		string expectedUrl = BaseUri.TrimEnd('/') + "/" + RelativeUrl.TrimStart('/');

		_mockHttp!.When(HttpMethod.Post, expectedUrl).Respond(HttpStatusCode.InternalServerError);

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		// Act & Assert.
		await Assert.ThrowsExceptionAsync<InternalServerErrorException>(async () =>
		{
			await _connection!.PostAsync(RelativeUrl, form);
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

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		// Act.
		_ = await _connection!.PostAsync(RelativeUrl, form);

		// Assert.
		Assert.AreEqual(ApplicationId, foundAppId, "application_id was not found in the form data.");
	}

	[TestMethod]
	public async Task PostAsync_ThrowsArgumentNullException_WhenFormIsNull()
	{
		// Act & Assert.
		await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
		{
			await _connection!.PostAsync(RelativeUrl, null);
		});
	}

	[TestMethod]
	[DataRow("INVALID_APPLICATION_ID")]
	[DataRow("INVALID_IP_ADDRESS")]
	[DataRow("APPLICATION_IS_BLOCKED")]
	public async Task PostAsync_ThrowsUnauthorizedAccessException_OnInvalidApplicationId(string errorMessage)
	{
		// Arrange.
		string expectedUrl = BaseUri.TrimEnd('/') + "/" + RelativeUrl.TrimStart('/');
		string errorJson = $$"""
		{
		  "status": "error",
		  "error": {
			"code": 407,
			"message": "{{errorMessage}}",
			"field": "application_id",
			"value": "test_app_id"
		  }
		}
		""";

		_mockHttp!.When(HttpMethod.Post, expectedUrl).Respond("application/json", errorJson);

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		// Act & Assert.
		UnauthorizedAccessException ex = await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(async () =>
		{
			await _connection!.PostAsync(RelativeUrl, form);
		});

		Assert.AreEqual(errorMessage, ex.Message);
	}

	[TestMethod]
	public async Task PostAsync_Retries_OnRateLimitExceeded()
	{
		// Arrange.
		string expectedUrl = BaseUri.TrimEnd('/') + "/" + RelativeUrl.TrimStart('/');
		string rateLimitJson = """
		{
			"status": "error",
			"error": {
				"code": 407,
				"message": "REQUEST_LIMIT_EXCEEDED",
				"field": "application_id",
				"value": null
			}
		}
		""";

		int retryCount = 3;
		_connection = new(_httpClient!, _loggerMock!, BaseUri, ApplicationId, maxRetries: retryCount);

		int callCount = 0;
		_mockHttp!.When(HttpMethod.Post, expectedUrl)
			.Respond(req =>
			{
				callCount++;
				return new HttpResponseMessage
				{
					Content = new StringContent(rateLimitJson),
					StatusCode = HttpStatusCode.OK
				};
			});

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		// Act & Assert.
		await Assert.ThrowsExceptionAsync<TimeoutException>(async () =>
		{
			await _connection!.PostAsync(RelativeUrl, form);
		});

		Assert.AreEqual(retryCount, callCount, "Expected retry count not met.");
	}

	[TestMethod]
	public async Task PostAsync_ThrowsInvalidOperationException_OnUndefinedErrorCode()
	{
		// Arrange.
		string expectedUrl = BaseUri.TrimEnd('/') + "/" + RelativeUrl.TrimStart('/');
		string errorJson = """
		{
			"status": "error",
			"error": {
				"code": 504,
				"message": "SOURCE_NOT_AVAILABLE",
				"field": "application_id",
				"value": null
			}
		}
		""";

		_mockHttp!.When(HttpMethod.Post, expectedUrl).Respond("application/json", errorJson);

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		// Act & Assert.
		InvalidOperationException ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
		{
			await _connection!.PostAsync(RelativeUrl, form);
		});

		Assert.AreEqual("API error 504: SOURCE_NOT_AVAILABLE", ex.Message);
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new WotbConnection(null, _loggerMock, BaseUri, ApplicationId));
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new WotbConnection(_httpClient, null, BaseUri, ApplicationId));
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new WotbConnection(_httpClient, _loggerMock, null, ApplicationId));
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new WotbConnection(_httpClient, _loggerMock, BaseUri, null));
	}
}
