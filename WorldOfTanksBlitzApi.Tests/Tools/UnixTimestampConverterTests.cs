namespace WorldOfTanksBlitzApi.Tests.Tools;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorldOfTanksBlitzApi.Tools;

[TestClass]
public class UnixTimestampConverterTests
{
	[TestMethod]
	public void Read_ValidUnixTimestamp_ReturnsCorrectDateTime()
	{
		// Arrange.
		string json = @"{""Timestamp"":1609459200}"; // 2021-01-01T00:00:00Z

		// Act.
		TimestampWrapper result = JsonSerializer.Deserialize<TimestampWrapper>(json)!;

		// Assert.
		Assert.AreEqual(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), result!.Timestamp);
	}

	[TestMethod]
	public void Write_DateTime_ReturnsCorrectUnixTimestamp()
	{
		// Arrange.
		TimestampWrapper value = new()
		{
			Timestamp = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc)
		};
		string expectedJson = @"{""Timestamp"":1609459200}";

		// Act.
		string json = JsonSerializer.Serialize(value);

		// Assert.
		Assert.AreEqual(expectedJson, json);
	}

	[TestMethod]
	public void Read_NullToken_ReturnsNull()
	{
		// Arrange.
		string json = @"{""Timestamp"":null}";

		// Act.
		TimestampWrapper result = JsonSerializer.Deserialize<TimestampWrapper>(json)!;

		// Assert.
		Assert.IsNull(result!.Timestamp);
	}

	[TestMethod]
	public void Write_NullValue_WritesJsonNull()
	{
		// Arrange.
		TimestampWrapper value = new()
		{
			Timestamp = null
		};
		string expectedJson = @"{""Timestamp"":null}";

		// Act.
		string json = JsonSerializer.Serialize(value);

		// Assert.
		Assert.AreEqual(expectedJson, json);
	}
}

public class TimestampWrapper
{
	[JsonConverter(typeof(UnixTimestampConverter))]
	public DateTime? Timestamp
	{
		get; set;
	}
}
