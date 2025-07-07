using System.Text.Json;
using System.Text.Json.Serialization;
using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Application.Helpers;

public class InsightCategoryJsonConverter : JsonConverter<InsightCategory>
{
    public override InsightCategory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value?.ToLowerInvariant() switch
        {
            "risco" => InsightCategory.Risco,
            "oportunidade" => InsightCategory.Oportunidade,
            "alerta" => InsightCategory.Alerta,
            "regulatory compliance" => InsightCategory.RegulatoryCompliance,
            "regulatorycompliance" => InsightCategory.RegulatoryCompliance,
            "outro" => InsightCategory.Outro,
            _ => InsightCategory.Outro
        };
    }

    public override void Write(Utf8JsonWriter writer, InsightCategory value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}