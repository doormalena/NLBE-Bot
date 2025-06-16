namespace NLBE_Bot.Tests.Helpers;

using DSharpPlus;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using System;
using System.Collections.Generic;
using System.Linq;

[TestClass()]
public class DiscordMessageExtensionsTests
{
	[TestMethod]
	public void SortReactionsTest()
	{
		// Arrange.
		DiscordConfiguration discordConfig = new()
		{
			Token = "dummy",
			TokenType = TokenType.Bot
		};
		DiscordClient discordClient = new(discordConfig);
		Mock<IErrorHandler> errorHandlerMock = new();
		Mock<ILogger<Worker>> loggerMock = new();
		DiscordMessageUtils utils = new(discordClient, errorHandlerMock.Object, loggerMock.Object);

		Mock<IDiscordEmoji> emoji1Mock = new();
		Mock<IDiscordEmoji> emoji2Mock = new();
		Mock<IDiscordUser> user1Mock = new();
		Mock<IDiscordReaction> reaction1Mock = new();
		reaction1Mock.Setup(r => r.Emoji).Returns(emoji1Mock.Object);
		Mock<IDiscordUser> user2Mock = new();
		Mock<IDiscordReaction> reaction2Mock = new();
		reaction2Mock.Setup(r => r.Emoji).Returns(emoji2Mock.Object);

		List<IDiscordReaction> reactionsList =
			[
				reaction1Mock.Object,
				reaction2Mock.Object
			];

		Mock<IDiscordMessage> messageMock = new();
		messageMock.Setup(r => r.Reactions).Returns(reactionsList);
		messageMock.Setup(r => r.GetReactionsAsync(emoji1Mock.Object)).ReturnsAsync([user1Mock.Object]);
		messageMock.Setup(r => r.GetReactionsAsync(emoji2Mock.Object)).ReturnsAsync([user2Mock.Object]);

		// Act.
		Dictionary<IDiscordEmoji, List<IDiscordUser>> result = utils.SortReactions(messageMock.Object);

		// Assert.
		Assert.AreEqual(2, result.Count);
		Assert.IsTrue(result[emoji1Mock.Object].Contains(user1Mock.Object));
		Assert.IsTrue(result[emoji2Mock.Object].Contains(user2Mock.Object));
	}

	[TestMethod]
	public void SortMessagesTest()
	{
		// Arrange.		
		DiscordConfiguration discordConfig = new()
		{
			Token = "dummy",
			TokenType = TokenType.Bot
		}
		;
		DiscordClient discordClient = new(discordConfig);
		Mock<IErrorHandler> errorHandlerMock = new();
		Mock<ILogger<Worker>> loggerMock = new();
		DiscordMessageUtils utils = new(discordClient, errorHandlerMock.Object, loggerMock.Object);

		// Example log format: "01-06-2024 12:34:56|rest"
		string content1 = "01-06-2024 12:34:56|something";
		string content2 = "01-06-2024 12:34:56|another";
		string content3 = "02-06-2024 13:00:00|different";
		Mock<IDiscordMessage> msg1 = new();
		msg1.SetupGet(m => m.Content).Returns(content1);
		Mock<IDiscordMessage> msg2 = new();
		msg2.SetupGet(m => m.Content).Returns(content2);
		Mock<IDiscordMessage> msg3 = new();
		msg3.SetupGet(m => m.Content).Returns(content3);

		List<IDiscordMessage> messages = [msg1.Object, msg2.Object, msg3.Object];

		// Act.
		Dictionary<DateTime, List<IDiscordMessage>> result = utils.SortMessages(messages);

		// Assert.
		Assert.AreEqual(2, result.Count);
		Assert.IsTrue(result.Values.Any(list => list.Contains(msg1.Object) && list.Contains(msg2.Object)));
		Assert.IsTrue(result.Values.Any(list => list.Contains(msg3.Object)));
	}
}
