namespace NLBE_Bot.Tests.Services;

using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using NLBE_Bot.Services;
using NSubstitute;

[TestClass]
public class BotStateTests
{
	[TestMethod]
	public async Task SaveAsync_CreatesFileWithCorrectContent()
	{
		// Arrange.		
		string tempFile = Path.GetTempFileName(); // Use a unique file for testing.

		_ = new BotState(tempFile)
		{
			IgnoreCommands = true,
			IgnoreEvents = true,
			WeeklyEventWinner = new WeeklyEventWinner { UserId = 42, LastEventDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
			LasTimeServerNicknamesWereVerified = new DateTime(2024, 2, 2, 0, 0, 0, DateTimeKind.Utc),
			LastWeeklyWinnerAnnouncement = new DateTime(2024, 3, 3, 0, 0, 0, DateTimeKind.Utc)
		};

		try
		{
			// Act.
			await Task.Delay(100); // Wait for async save to complete

			// Assert.
			Assert.IsTrue(File.Exists(tempFile), "State file should exist after SaveAsync.");
			string json = await File.ReadAllTextAsync(tempFile);
			Assert.IsTrue(json.Contains("IgnoreCommands"));
			Assert.IsTrue(json.Contains("IgnoreEvents"));
			Assert.IsTrue(json.Contains("WeeklyEventWinner"));
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[TestMethod]
	public async Task LoadAsync_LoadsStateCorrectly()
	{
		// Arrange.
		string tempFile = Path.GetTempFileName();
		string json = """
        {
            "IgnoreCommands": true,
            "IgnoreEvents": false,
            "WeeklyEventWinner": { "UserId": 99, "LastEventDate": "2024-01-01T00:00:00" },
            "LasTimeServerNicknamesWereVerified": "2024-02-02T00:00:00",
            "LastWeeklyWinnerAnnouncement": "2024-03-03T00:00:00"
        }
        """;
		await File.WriteAllTextAsync(tempFile, json);
		BotState state = new(tempFile);

		// Act.
		await state.LoadAsync();

		// Assert.
		Assert.IsTrue(state.IgnoreCommands);
		Assert.IsFalse(state.IgnoreEvents);
		Assert.AreEqual(99UL, state.WeeklyEventWinner.UserId);
		Assert.AreEqual(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), state.WeeklyEventWinner.LastEventDate);
		Assert.AreEqual(new DateTime(2024, 2, 2, 0, 0, 0, DateTimeKind.Utc), state.LasTimeServerNicknamesWereVerified);
		Assert.AreEqual(new DateTime(2024, 3, 3, 0, 0, 0, DateTimeKind.Utc), state.LastWeeklyWinnerAnnouncement);

		File.Delete(tempFile);
	}

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
		WeeklyEventWinner weeklyEventWinner = new()
		{
			UserId = 123UL,
			LastEventDate = DateTime.UtcNow
		};
		BotState state = new()
		{
			WeeklyEventWinner = weeklyEventWinner
		};
		Assert.AreEqual(weeklyEventWinner, state.WeeklyEventWinner);
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

	[TestMethod]
	public void WeeklyEventWinner_IsThreadSafe()
	{
		BotState state = new();
		int exceptions = 0;
		WeeklyEventWinner winner = new()
		{
			UserId = 1,
			LastEventDate = DateTime.UtcNow
		};

		Parallel.For(0, 10000, i =>
		{
			try
			{
				if (i % 2 == 0)
				{
					state.WeeklyEventWinner = winner;
				}
				else
				{
					_ = state.WeeklyEventWinner;
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
