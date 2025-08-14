namespace WorldOfTanksBlitzApi.Tools;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class UnixTimestampNullableConverter : JsonConverter<DateTime?>
{
	public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}

		long seconds = reader.GetInt64();
		return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
	}

	public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
	{
		if (value is null)
		{
			writer.WriteNullValue();
			return;
		}

		long unixTime = new DateTimeOffset(value.Value).ToUnixTimeSeconds();
		writer.WriteNumberValue(unixTime);
	}
}
