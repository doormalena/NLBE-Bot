namespace NLBE_Bot.Helpers.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;

[TestClass()]
public class PublicIpAddressTests
{
	[TestMethod]
	public async Task GetPublicIpAddressAsync_ReturnsIp_WhenApiReturnsValidJson()
	{
		// Arrange
		MockHttpMessageHandler mockHttp = new();
		mockHttp.When("https://api.ipify.org*").Respond("application/json", "{\"ip\":\"1.2.3.4\"}");
		HttpClient client = new(mockHttp);

		// Act
		string result = await PublicIpAddress.GetPublicIpAddressAsync(client);

		// Assert
		Assert.AreEqual("1.2.3.4", result);
	}

	[TestMethod]
	public async Task GetPublicIpAddressAsync_ReturnsErrorMessage_OnException()
	{
		// Arrange
		string error = "Network error";
		MockHttpMessageHandler mockHttp = new();
		mockHttp.When("https://api.ipify.org*").Throw(new HttpRequestException(error));
		HttpClient client = new(mockHttp);

		// Act
		string result = await PublicIpAddress.GetPublicIpAddressAsync(client);

		// Assert
		StringAssert.StartsWith(result, "Unable to retrieve IP, cause:");
		StringAssert.Contains(result, error);
	}
}
