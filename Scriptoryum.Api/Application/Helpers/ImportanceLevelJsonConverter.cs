using System.Text.Json;
using System.Text.Json.Serialization;
using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Application.Helpers;

public class ImportanceLevelJsonConverter : JsonConverter<ImportanceLevel>
{
    public override ImportanceLevel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value?.ToLowerInvariant() switch
        {
            "low" or "baixo" => ImportanceLevel.Low,
            "medium" or "médio" or "medio" => ImportanceLevel.Medium,
            "high" or "alto" => ImportanceLevel.High,
            "critical" or "crítico" or "critico" => ImportanceLevel.Critical,
            _ => ImportanceLevel.Low // default/fallback
        };
    }

    public override void Write(Utf8JsonWriter writer, ImportanceLevel value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}