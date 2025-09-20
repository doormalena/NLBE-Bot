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
		string result = await _connection!.UploadReplayAsync(RelativeUrl, ExpectedFileName, ExpectedFileContent, "Battle Title");

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
		}
	}

	[TestMethod]
	public async Task UploadReplayAsync_ThrowsInternalServerErrorException_OnServerError()
	{
		// Arrange.
		_mockHttp!.When($"{BaseUri}/{RelativeUrl}")
				.Respond(HttpStatusCode.InternalServerError);

		// Act & Assert.
		await Assert.ThrowsExceptionAsync<InternalServerErrorException>(async () =>
		{
			await _connection!.UploadReplayAsync(RelativeUrl, ExpectedFileName, ExpectedFileContent, "Title");
		});
	}

	[TestMethod]
	public async Task UploadReplayAsync_ThrowsInvalidOperationException_OnMalformedJson()
	{
		// Arrange.
		string malformedJson = "{ \"status\": \"ok\", \"data\": [ "; // Incomplete JSON

		_mockHttp!.When($"{BaseUri}/{RelativeUrl}")
			.Respond(req => new HttpResponseMessage(HttpStatusCode.BadRequest)
			{
				Content = new StringContent(malformedJson, Encoding.UTF8, "application/json")
			});

		// Act & Assert.
		InvalidOperationException ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
		{
			await _connection!.UploadReplayAsync(RelativeUrl, ExpectedFileName, ExpectedFileContent, "Title");
		});

		Assert.AreEqual("Failed to parse API response.", ex.Message);
	}

	[TestMethod]
	public async Task UploadReplayAsync_ThrowsHttpRequestException_OnValidationErrorJson()
	{
		// Arrange.
		const string errorJson = @"{
		  ""title"": [""This field may not be null.""],
		  ""upload_file"": [""The submitted data was not a file. Check the encoding type on the form.""]
		}";

		_mockHttp!.When(HttpMethod.Post, $"{BaseUri}/{RelativeUrl}")
			.Respond(req => new HttpResponseMessage(HttpStatusCode.BadRequest)
			{
				Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
			});

		// Act & Assert.
		HttpRequestException ex = await Assert.ThrowsExceptionAsync<HttpRequestException>(async () =>
		{
			await _connection!.UploadReplayAsync(RelativeUrl, ExpectedFileName, ExpectedFileContent, null!);
		});

		Assert.IsTrue(ex.Message.Contains("title: This field may not be null."));
		Assert.IsTrue(ex.Message.Contains("upload_file: The submitted data was not a file. Check the encoding type on the form."));
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
