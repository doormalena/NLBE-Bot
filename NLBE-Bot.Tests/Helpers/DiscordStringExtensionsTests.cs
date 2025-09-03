namespace NLBE_Bot.Tests.Helpers;

using NLBE_Bot.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class DiscordStringExtensionsTests
{
	[TestInitialize]
	public void Setup()
	{
		// No dependencies to initialize for static extension methods
	}

	[TestMethod]
	public void AdaptToChat_ShouldEscapeUnderscores()
	{
		// Arrange.
		string input = "VK_72.01_(K)";

		// Act.
		string result = input.AdaptToChat();

		// Assert.
		Assert.AreEqual("VK\\_72.01\\_(K)", result);
	}

	[TestMethod]
	public void AdaptToChat_ShouldUnescapeDoubleEscapedUnderscores()
	{
		// Arrange.
		string input = "VK\\_72.01\\_(K)";

		// Act.
		string result = input.AdaptToChat();

		// Assert.
		Assert.AreEqual("VK\\_72.01\\_(K)", result); // No change since it's already escaped
	}

	[TestMethod]
	public void AdaptToChat_ShouldReturnEmpty_WhenInputIsNull()
	{
		// Arrange.
		string? input = null;

		// Act.
		string result = input!.AdaptToChat();

		// Assert.
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void AdaptLink_ShouldConvertUrlsToMarkdownLinks()
	{
		// Arrange.
		string input = "Check this link:\nhttps://example.com/page";

		// Act.
		string result = input.AdaptLink();

		// Assert.
		Assert.IsTrue(result.Contains("[https://example.com/page](https://example.com/page)"));
	}

	[TestMethod]
	public void AddaptLink_ShouldReturnEmpty_WhenInputIsNull()
	{
		// Arrange.
		string? input = null;

		// Act.
		string result = input!.AdaptLink();

		// Assert.
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void AdaptMutlipleLines_ShouldCollapseExcessiveLineBreaks()
	{
		// Arrange.
		string input = "Line1\n\n\nLine2\r\r\rLine3\r\n\rLine4";

		// Act.
		string result = input.AdaptMutlipleLines();

		// Assert.
		Assert.IsFalse(result.Contains("\n\n\n"));
		Assert.IsFalse(result.Contains("\r\r\r"));
		Assert.IsTrue(result.Contains("\n\n")); // Collapsed form
	}

	[TestMethod]
	public void AdaptMutlipleLines_ShouldReturnOriginal_WhenNoExtraBreaks()
	{
		// Arrange.
		string input = "Line1\nLine2";

		// Act.
		string result = input.AdaptMutlipleLines();

		// Assert.
		Assert.AreEqual("Line1\nLine2", result);
	}
}
