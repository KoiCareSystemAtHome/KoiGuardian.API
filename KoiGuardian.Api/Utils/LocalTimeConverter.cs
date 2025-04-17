using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KoiGuardian.Api.Utils;

public class LocalTimeDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Local);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
}
