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

[TestClass]
public class WotInspectorConnectionTests
{
	private const string BaseUri = "https://api.test.com";
	private const string RelativeUrl = "replay/upload";
	private readonly byte[] ExpectedFileContent = [1, 2, 3, 4];
	private const string ExpectedFileName = "file.wotbreplay";
	private const string ExpectedResponse = "{\"status\":\"ok\"}";

	private MockHttpMessageHandler? _mockHttp;
	private HttpClient? _httpClient;
	private ILogger<WotInspectorConnection>? _loggerMock;
	private WotInspectorConnection? _connection;

	[TestInitialize]
	public void Setup()
	{
		_mockHttp = new();
		_httpClient = _mockHttp.ToHttpClient();
		_loggerMock = Substitute.For<ILogger<WotInspectorConnection>>();
		_connection = new(_httpClient, _loggerMock, BaseUri);
	}

	[TestMethod]
	public async Task UploadReplayAsync_WithAllParameters_SendsCorrectFormFields_And_ReturnsExpectedJson()
	{
		// Arrange.
		HttpRequestMessage? capturedRequest = null;
		_mockHttp!.When(HttpMethod.Post, $"{BaseUri}/{RelativeUrl}")
				.Respond(req =>
				{
					capturedRequest = req;
					return new HttpResponseMessage
					{
						Content = new StringContent(ExpectedResponse, Encoding.UTF8, "application/json")
					};
				});

		// Act.
		string result = await _connection!.UploadReplayAsync(RelativeUrl, ExpectedFileName, ExpectedFileContent, "Battle Title", 123456);

		// Assert.
		Assert.AreEqual(ExpectedResponse, result);
		Assert.IsNotNull(capturedRequest);
		MultipartFormDataContent? formData = capturedRequest.Content as MultipartFormDataContent;
		Assert.IsNotNull(formData);

		foreach (HttpContent part in formData)
		{
			Assert.IsNotNull(part.Headers.ContentDisposition);
			string? name = part.Headers.ContentDisposition.Name?.Trim('"');
			string? value = await part.ReadAsStringAsync();

			Assert.IsNotNull(name);
			Assert.IsFalse(string.IsNullOrWhiteSpace(value));

			if (name == "filename")
			{
				Assert.AreEqual("file.wotbreplay", value);
			}

			if (name == "title")
			{
				Assert.AreEqual("Battle Title", value);
			}

			if (name == "loaded_by")
			{
				Assert.AreEqual("123456", value);
			}
		}
	}

	[TestMethod]
	public async Task UploadReplayAsync_WithNullTitleAndAccountId_ReturnsExpectedJson()
	{
		// Arrange.
		_mockHttp!.When(HttpMethod.Post, $"{BaseUri}/{RelativeUrl}")
				 .Respond("application/json", ExpectedResponse);

		// Act.
		string result = await _connection!.UploadReplayAsync(RelativeUrl, ExpectedFileName, ExpectedFileContent, null, null);

		// Assert.
		Assert.AreEqual(ExpectedResponse, result);
	}

	[TestMethod]
	public async Task UploadReplayAsync_LogsDebugMessage()
	{
		// Arrange.
		_mockHttp!.When(HttpMethod.Post, $"{BaseUri}/{RelativeUrl}")
				 .Respond("application/json", ExpectedResponse);

		// Act.
		await _connection!.UploadReplayAsync(RelativeUrl, ExpectedFileName, ExpectedFileContent, "Battle Title", 123456);

		// Assert.
		_loggerMock!.Received().Log(
			LogLevel.Debug,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains(ExpectedFileName)),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	[TestMethod]
	public async Task UploadReplayAsync_ThrowsException_WhenPostFails()
	{
		// Arrange.
		_mockHttp!.When($"{BaseUri}/{RelativeUrl}")
				.Respond(HttpStatusCode.InternalServerError);

		// Act & Assert.
		await Assert.ThrowsExceptionAsync<HttpRequestException>(async () =>
		{
			await _connection!.UploadReplayAsync(RelativeUrl, ExpectedFileName, ExpectedFileContent, "Title", 123);
		});
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new WotInspectorConnection(null!, _loggerMock!, BaseUri!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new WotInspectorConnection(_httpClient!, null!, BaseUri!));
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new WotInspectorConnection(_httpClient!, _loggerMock!, null!));
	}
}
