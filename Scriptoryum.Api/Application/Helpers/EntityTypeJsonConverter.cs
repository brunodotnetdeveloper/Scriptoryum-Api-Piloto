using Scriptoryum.Api.Domain.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scriptoryum.Api.Application.Helpers;

public class EntityTypeJsonConverter : JsonConverter<EntityType>
{
    public override EntityType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            return EntityType.Other;

        return value.Trim().ToLowerInvariant() switch
        {
            "pessoa" => EntityType.Person,
            "empresa" => EntityType.Organization,
            "data" => EntityType.Date,
            "valor" => EntityType.Value,
            "local" => EntityType.Location,
            "documento" => EntityType.Document,
            "outro" => EntityType.Other,
            _ => EntityType.Other
        };
    }

    public override void Write(Utf8JsonWriter writer, EntityType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}