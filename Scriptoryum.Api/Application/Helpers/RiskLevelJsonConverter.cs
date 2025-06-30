using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Application.Helpers;

public class RiskLevelJsonConverter : JsonConverter<RiskLevel>
{
    public override RiskLevel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            return RiskLevel.Unknown;

        return value.Trim().ToLowerInvariant() switch
        {
            "low" => RiskLevel.Low,
            "medium" => RiskLevel.Medium,
            "high" => RiskLevel.High,
            "critical" => RiskLevel.Critical,
            _ => RiskLevel.Unknown
        };
    }

    public override void Write(Utf8JsonWriter writer, RiskLevel value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}