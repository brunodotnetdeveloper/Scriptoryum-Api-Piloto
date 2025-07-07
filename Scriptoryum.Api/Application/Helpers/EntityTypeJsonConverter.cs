//using Scriptoryum.Api.Domain.Enums;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace Scriptoryum.Api.Application.Helpers;

//public class EntityTypeJsonConverter : JsonConverter<EntityType>
//{
//    public override EntityType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//    {
//        var value = reader.GetString();
//        if (string.IsNullOrWhiteSpace(value))
//            return EntityType.Outro;

//        return value.Trim().ToLowerInvariant() switch
//        {
//            "pessoa" => EntityType.Pessoa,
//            "empresa" => EntityType.Organizacao,
//            "organizacao" => EntityType.Organizacao,
//            "data" => EntityType.Data,
//            "valor" => EntityType.Valor,
//            "local" => EntityType.Localizacao,
//            "localizacao" => EntityType.Localizacao,
//            "documento" => EntityType.Documento,
//            "outro" => EntityType.Outro,
//            _ => EntityType.Outro
//        };
//    }

//    public override void Write(Utf8JsonWriter writer, EntityType value, JsonSerializerOptions options)
//    {
//        writer.WriteStringValue(value.ToString());
//    }
//}