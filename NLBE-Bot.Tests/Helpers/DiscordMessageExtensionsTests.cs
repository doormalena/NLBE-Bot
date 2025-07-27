namespace NLBE_Bot.Tests.Helpers;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NSubstitute;
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
		IDiscordClient discordClient = Substitute.For<IDiscordClient>();
		ILogger<DiscordMessageUtils> loggerMock = Substitute.For<ILogger<DiscordMessageUtils>>();
		DiscordMessageUtils utils = new(discordClient, loggerMock);

		IDiscordEmoji emoji1Mock = Substitute.For<IDiscordEmoji>();
		IDiscordEmoji emoji2Mock = Substitute.For<IDiscordEmoji>();
		IDiscordUser user1Mock = Substitute.For<IDiscordUser>();
		IDiscordReaction reaction1Mock = Substitute.For<IDiscordReaction>();
		reaction1Mock.Emoji.Returns(emoji1Mock);
		IDiscordUser user2Mock = Substitute.For<IDiscordUser>();
		IDiscordReaction reaction2Mock = Substitute.For<IDiscordReaction>();
		reaction2Mock.Emoji.Returns(emoji2Mock);

		List<IDiscordReaction> reactionsList =
		[
			reaction1Mock,
			reaction2Mock
		];

		IDiscordMessage messageMock = Substitute.For<IDiscordMessage>();
		messageMock.Reactions.Returns(reactionsList);
		messageMock.GetReactionsAsync(emoji1Mock).Returns([user1Mock]);
		messageMock.GetReactionsAsync(emoji2Mock).Returns([user2Mock]);

		// Act.
		Dictionary<IDiscordEmoji, List<IDiscordUser>> result = utils.SortReactions(messageMock);

		// Assert.
		Assert.AreEqual(2, result.Count);
		Assert.IsTrue(result[emoji1Mock].Contains(user1Mock));
		Assert.IsTrue(result[emoji2Mock].Contains(user2Mock));
	}

	[TestMethod]
	public void SortMessagesTest()
	{
		// Arrange.		
		IDiscordClient discordClient = Substitute.For<IDiscordClient>();
		ILogger<DiscordMessageUtils> loggerMock = Substitute.For<ILogger<DiscordMessageUtils>>();
		DiscordMessageUtils utils = new(discordClient, loggerMock);

		// Example log format: "01-06-2024 12:34:56|rest"
		string content1 = "01-06-2024 12:34:56|something";
		string content2 = "01-06-2024 12:34:56|another";
		string content3 = "02-06-2024 13:00:00|different";
		IDiscordMessage msg1 = Substitute.For<IDiscordMessage>();
		msg1.Content.Returns(content1);
		IDiscordMessage msg2 = Substitute.For<IDiscordMessage>();
		msg2.Content.Returns(content2);
		IDiscordMessage msg3 = Substitute.For<IDiscordMessage>();
		msg3.Content.Returns(content3);

		List<IDiscordMessage> messages = [msg1, msg2, msg3];

		// Act.
		Dictionary<DateTime, List<IDiscordMessage>> result = utils.SortMessages(messages);

		// Assert.
		Assert.AreEqual(2, result.Count);
		Assert.IsTrue(result.Values.Any(list => list.Contains(msg1) && list.Contains(msg2)));
		Assert.IsTrue(result.Values.Any(list => list.Contains(msg3)));
	}
}
