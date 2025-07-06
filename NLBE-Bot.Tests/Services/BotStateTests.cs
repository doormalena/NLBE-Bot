namespace NLBE_Bot.Tests.Services;

using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using NSubstitute;

[TestClass]
public class BotStateTests
{
	[TestMethod]
	public void IgnoreCommands_GetSet_Works()
	{
		BotState state = new()
		{
			IgnoreCommands = true
		};
		Assert.IsTrue(state.IgnoreCommands);

		state.IgnoreCommands = false;
		Assert.IsFalse(state.IgnoreCommands);
	}

	[TestMethod]
	public void IgnoreEvents_GetSet_Works()
	{
		BotState state = new()
		{
			IgnoreEvents = true
		};
		Assert.IsTrue(state.IgnoreEvents);

		state.IgnoreEvents = false;
		Assert.IsFalse(state.IgnoreEvents);
	}

	[TestMethod]
	public void WeeklyEventWinner_GetSet_Works()
	{
		Tuple<ulong, DateTime> tuple = new(123UL, DateTime.UtcNow);
		BotState state = new()
		{
			WeeklyEventWinner = tuple
		};
		Assert.AreEqual(tuple, state.WeeklyEventWinner);
	}

	[TestMethod]
	public void LastCreatedDiscordMessage_GetSet_Works()
	{
		BotState state = new();
		IDiscordMessage message = Substitute.For<IDiscordMessage>();
		state.LastCreatedDiscordMessage = message;
		Assert.AreEqual(message, state.LastCreatedDiscordMessage);
	}

	[TestMethod]
	public void LasTimeNamesWereUpdated_GetSet_Works()
	{
		DateTime now = DateTime.UtcNow;
		BotState state = new()
		{
			LasTimeServerNicknamesWereVerified = now
		};
		Assert.AreEqual(now, state.LasTimeServerNicknamesWereVerified);

		state.LasTimeServerNicknamesWereVerified = null;
		Assert.IsNull(state.LasTimeServerNicknamesWereVerified);
	}

	[TestMethod]
	public void LastWeeklyWinnerAnnouncement_GetSet_Works()
	{
		DateTime now = DateTime.UtcNow;
		BotState state = new()
		{
			LastWeeklyWinnerAnnouncement = now
		};
		Assert.AreEqual(now, state.LastWeeklyWinnerAnnouncement);

		state.LastWeeklyWinnerAnnouncement = null;
		Assert.IsNull(state.LastWeeklyWinnerAnnouncement);
	}

	[TestMethod]
	public void IgnoreCommands_IsThreadSafe()
	{
		BotState state = new();
		int exceptions = 0;

		Parallel.For(0, 10000, i =>
		{
			try
			{
				if (i % 2 == 0)
				{
					state.IgnoreCommands = true;
				}
				else
				{
					_ = state.IgnoreCommands;
				}
			}
			catch
			{
				Interlocked.Increment(ref exceptions);
			}
		});

		Assert.AreEqual(0, exceptions, "No exceptions should be thrown during concurrent access.");
	}
}
