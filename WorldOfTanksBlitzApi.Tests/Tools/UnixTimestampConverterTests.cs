namespace WorldOfTanksBlitzApi.Tests.Tools;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;
using WorldOfTanksBlitzApi.Tools;

[TestClass]
public class UnixTimestampConverterTests
{
	private readonly JsonSerializerOptions _options = new()
	{
		Converters = { new UnixTimestampNullableConverter() }
	};

	[TestMethod]
	public void Read_ValidUnixTimestamp_ReturnsCorrectDateTime()
	{
		// Arrange.
		long unixTimestamp = 1609459200; // 2021-01-01T00:00:00Z
		string json = unixTimestamp.ToString();

		// Act.
		DateTime? result = JsonSerializer.Deserialize<DateTime?>(json, _options);

		// Assert.
		Assert.AreEqual(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
	}

	[TestMethod]
	public void Write_DateTime_ReturnsCorrectUnixTimestamp()
	{
		// Arrange.
		DateTime? dateTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		string expectedJson = "1609459200";

		// Act.
		string json = JsonSerializer.Serialize(dateTime, _options);

		// Assert.
		Assert.AreEqual(expectedJson, json);
	}

	[TestMethod]
	public void Read_NullToken_ReturnsNull()
	{
		// Arrange.
		string json = "null";

		// Act.
		DateTime? result = JsonSerializer.Deserialize<DateTime?>(json, _options);

		// Assert.
		Assert.IsNull(result);
	}

	[TestMethod]
	public void Write_NullValue_WritesJsonNull()
	{
		// Arrange.
		DateTime? value = null;

		// Act.
		string json = JsonSerializer.Serialize(value, _options);

		// Assert.
		Assert.AreEqual("null", json);
	}
}
