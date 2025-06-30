using System.Text.Json;
using System.Text.Json.Serialization;
using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Application.Helpers;

public class TimelineEventTypeJsonConverter : JsonConverter<TimelineEventType>
{
    public override TimelineEventType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string value = reader.GetString();
        if (string.IsNullOrEmpty(value)) return TimelineEventType.Other;

        // Try to parse ignoring case
        if (Enum.TryParse(value, true, out TimelineEventType result))
            return result;

        // Handle special cases like accents and variations
        return value.ToLower() switch
        {
            "audiência" or "audiencia" => TimelineEventType.Audience,
            "sentença" or "sentenca" => TimelineEventType.Sentence,
            "citação" or "citacao" => TimelineEventType.Quote,
            "intimação" or "intimacao" => TimelineEventType.Summons,
            "publicação" or "publicacao" => TimelineEventType.Publication,
            _ => TimelineEventType.Other
        };
    }

    public override void Write(Utf8JsonWriter writer, TimelineEventType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}