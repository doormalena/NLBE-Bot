namespace NLBE_Bot.Tests.Services;

using FMWOTB.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using NLBE_Bot.Services;
using NSubstitute;

[TestClass]
public class UserServiceTests
{
	private IMessageService? _messageServcieMock;
	private ILogger<UserService>? _loggerMock;
	private IAccountsRepository? _accountRepositoryMock;
	private UserService? _userService;

	[TestInitialize]
	public void Setup()
	{
		_messageServcieMock = Substitute.For<IMessageService>();
		_loggerMock = Substitute.For<ILogger<UserService>>();
		_accountRepositoryMock = Substitute.For<IAccountsRepository>();
		_userService = new UserService(_loggerMock, _messageServcieMock, _accountRepositoryMock);
	}

	[TestMethod]
	[DataRow("[TAG] PlayerName", "[TAG]", "PlayerName")]
	[DataRow("[NLBE] JohnDoe", "[NLBE]", "JohnDoe")]
	[DataRow("NoClanTagName", "", "NoClanTagName")]
	[DataRow("[CLAN] Name With Spaces", "[CLAN]", "Name With Spaces")]
	[DataRow("[CLAN]NameNoSpace", "[CLAN]", "NameNoSpace")]
	[DataRow("[CLAN] ", "[CLAN]", "")]
	[DataRow("", "", "")]
	public void GetWotbPlayerNameFromDisplayName_ParsesCorrectly(string displayName, string expectedClanTag, string expectedPlayerName)
	{
		// Act.
		WotbPlayerNameInfo result = _userService!.GetWotbPlayerNameFromDisplayName(displayName);

		// Assert.
		Assert.AreEqual(expectedClanTag, result.ClanTag);
		Assert.AreEqual(expectedPlayerName, result.PlayerName);
	}
}
