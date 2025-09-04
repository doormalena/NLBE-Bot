namespace WorldOfTanksBlitzApi.Tests;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using RichardSzalay.MockHttp;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
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
		HttpRequestMessage capturedRequest = null;
		_mockHttp!.When(HttpMethod.Post, expectedUrl)
				.Respond(req =>
				{
					capturedRequest = req;
					return new HttpResponseMessage
					{
						Content = new StringContent(ExpectedContent, Encoding.UTF8, "application/json")
					};
				});

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		// Act.
		_ = await _connection!.PostAsync(RelativeUrl, form);

		Assert.IsNotNull(capturedRequest);
		MultipartFormDataContent formData = capturedRequest.Content as MultipartFormDataContent;
		Assert.IsNotNull(formData);

		foreach (HttpContent part in formData)
		{
			if (part is StringContent stringContent)
			{
				string value = await stringContent.ReadAsStringAsync();
				if (value == ApplicationId)
				{
					foundAppId = value;
					break;
				}
			}
		}
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
	public async Task PostAsync_LogsBackoffDelay_OnRateLimitExceeded()
	{
		// Arrange.
		string expectedUrl = BaseUri.TrimEnd('/') + "/" + RelativeUrl.TrimStart('/');
		string errorJson = """
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

		TimeSpan? capturedDelay = null;
		Task fakeDelay(TimeSpan delay)
		{
			capturedDelay = delay;
			return Task.CompletedTask;
		}

		_connection = new(_httpClient!, _loggerMock, BaseUri, ApplicationId, maxRetries: 1, delayFunc: fakeDelay);

		_mockHttp!.When(HttpMethod.Post, expectedUrl).Respond("application/json", errorJson);

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		// Act & Assert.
		await Assert.ThrowsExceptionAsync<TimeoutException>(async () =>
		{
			await _connection!.PostAsync(RelativeUrl, form);
		});
		Assert.IsNotNull(capturedDelay, "Backoff delay was not triggered.");
		_loggerMock!.Received().Log(
			LogLevel.Debug,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("Rate limit exceeded")),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task PostAsync_UsesExponentialBackoff_OnMultipleRateLimitResponses()
	{
		// Arrange
		string expectedUrl = BaseUri.TrimEnd('/') + "/" + RelativeUrl.TrimStart('/');
		string errorJson = """
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

		List<TimeSpan> delays = [];
		Task fakeDelay(TimeSpan delay)
		{
			delays.Add(delay);
			return Task.CompletedTask;
		}

		int maxRetries = 4;
		_loggerMock = Substitute.For<ILogger<WotbConnection>>();
		_connection = new(_httpClient!, _loggerMock, BaseUri, ApplicationId, maxRetries, fakeDelay);

		_mockHttp!.When(HttpMethod.Post, expectedUrl).Respond("application/json", errorJson);

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		// Act & Assert.
		await Assert.ThrowsExceptionAsync<TimeoutException>(async () =>
		{
			await _connection!.PostAsync(RelativeUrl, form);
		});

		Assert.IsTrue(delays.Count > 0, "No delays were recorded. Retry logic may not have been triggered.");
		Assert.AreEqual(maxRetries, delays.Count, "Unexpected number of backoff attempts.");

		// Verify exponential growth pattern
		for (int i = 0; i < delays.Count; i++)
		{
			double maxExpectedSeconds = 0.5 * Math.Pow(2, i);
			Assert.IsTrue(delays[i].TotalSeconds >= 0, $"Delay at attempt {i} was negative.");
			Assert.IsTrue(delays[i].TotalSeconds <= maxExpectedSeconds,
				$"Delay at attempt {i} ({delays[i].TotalSeconds:F2}s) exceeded max expected ({maxExpectedSeconds:F2}s).");
		}

		// Print delays for visual inspection.
		foreach ((TimeSpan delay, int i) in delays.Select((d, i) => (d, i)))
		{
			double maxExpected = 0.5 * Math.Pow(2, i);
			Console.WriteLine($"Attempt {i}: Delay = {delay.TotalSeconds:F3}s (Max Expected: {maxExpected:F3}s)");
		}
	}
	[TestMethod]
	public async Task PostAsync_StopsRetrying_AfterSuccessfulResponse()
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

		string successJson = """
		{
			"status": "ok",
			"data": []
		}
		""";

		int failAttempts = 2;
		int totalCalls = 0;

		_mockHttp!.When(HttpMethod.Post, expectedUrl)
			.Respond(_ =>
			{
				string responseJson = totalCalls < failAttempts ? rateLimitJson : successJson;
				totalCalls++;
				return new HttpResponseMessage
				{
					Content = new StringContent(responseJson),
					StatusCode = HttpStatusCode.OK
				};
			});

		List<TimeSpan> delays = [];
		Task fakeDelay(TimeSpan delay)
		{
			delays.Add(delay);
			return Task.CompletedTask;
		}

		_loggerMock = Substitute.For<ILogger<WotbConnection>>();
		_connection = new(_httpClient!, _loggerMock, BaseUri, ApplicationId, maxRetries: 5, delayFunc: fakeDelay);

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		// Act.
		string result = await _connection!.PostAsync(RelativeUrl, form);

		// Assert.
		Assert.AreEqual(successJson, result, "Did not return expected successful response.");
		Assert.AreEqual(failAttempts, delays.Count, "Unexpected number of retries before success.");
		Assert.AreEqual(failAttempts + 1, totalCalls, "Unexpected number of total HTTP calls.");
	}

	[TestMethod]
	public async Task PostAsync_ThrowsInvalidOperationException_OnMalformedJson()
	{
		// Arrange.
		string expectedUrl = BaseUri.TrimEnd('/') + "/" + RelativeUrl.TrimStart('/');
		string malformedJson = "{ \"status\": \"ok\", \"data\": [ "; // Incomplete JSON

		_mockHttp!.When(HttpMethod.Post, expectedUrl).Respond("application/json", malformedJson);

		MultipartFormDataContent form = [];
		form.Add(new StringContent("value"), "key");

		// Act & Assert.
		InvalidOperationException ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
		{
			await _connection!.PostAsync(RelativeUrl, form);
		});

		Assert.AreEqual("Failed to parse API response.", ex.Message);
	}

	[TestMethod]
	[DataRow(407, "%FIELD%_LIST_LIMIT_EXCEEDED")]
	[DataRow(504, "SOURCE_NOT_AVAILABLE")]
	public async Task PostAsync_ThrowsInvalidOperationException_OnUndefinedErrorCode(int errorCode, string message)
	{
		// Arrange.
		string expectedUrl = BaseUri.TrimEnd('/') + "/" + RelativeUrl.TrimStart('/');
		string errorJson = $$"""
		{
			"status": "error",
			"error": {
				"code": {{errorCode}},
				"message": "{{message}}",
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

		Assert.AreEqual($"API error {errorCode}: {message}", ex.Message);
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new WotbConnection(null!, _loggerMock!, BaseUri!, ApplicationId!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new WotbConnection(_httpClient!, null!, BaseUri!, ApplicationId!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new WotbConnection(_httpClient!, _loggerMock!, null!, ApplicationId!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new WotbConnection(_httpClient!, _loggerMock!, BaseUri!, null!));
	}
}
