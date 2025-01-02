using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KoiGuardian.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ParameterImpactType
    {
        Increased,
        Decreased
    }

    public class ParameterImpact
    {
        public string ParameterId { get; set; }
        public ParameterImpactType ImpactType { get; set; }
    }

    public class ParameterImpactConverter : JsonConverter<Dictionary<string, ParameterImpactType>>
    {
        public override Dictionary<string, ParameterImpactType> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var impacts = new Dictionary<string, ParameterImpactType>();

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected start of object");
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected property name");
                }

                var parameterId = reader.GetString();
                reader.Read();

                // Handle string value for enum
                if (reader.TokenType == JsonTokenType.String)
                {
                    var enumString = reader.GetString();
                    if (Enum.TryParse<ParameterImpactType>(enumString, true, out var impactType))
                    {
                        impacts.Add(parameterId, impactType);
                    }
                    else
                    {
                        throw new JsonException($"Invalid ParameterImpactType value: {enumString}");
                    }
                }
                else
                {
                    throw new JsonException("Expected string value for ParameterImpactType");
                }
            }

            return impacts;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, ParameterImpactType> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var impact in value)
            {
                writer.WritePropertyName(impact.Key);
                writer.WriteStringValue(impact.Value.ToString());
            }

            writer.WriteEndObject();
        }
    }
}