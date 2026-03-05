using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CompraProgramadaWebApp.Utils.Json
{
    public class DecimalTwoPlacesConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var d))
                return Math.Round(d, 2);

            if (reader.TokenType == JsonTokenType.String && decimal.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                return Math.Round(v, 2);

            return 0m;
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            var formatted = Math.Round(value, 2).ToString("F2", CultureInfo.InvariantCulture);
            // Write as raw number preserving two decimal places
            writer.WriteRawValue(formatted);
        }
    }

    public class NullableDecimalTwoPlacesConverter : JsonConverter<decimal?>
    {
        public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var d))
                return Math.Round(d, 2);
            if (reader.TokenType == JsonTokenType.String && decimal.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                return Math.Round(v, 2);
            return null;
        }

        public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            var formatted = Math.Round(value.Value, 2).ToString("F2", CultureInfo.InvariantCulture);
            writer.WriteRawValue(formatted);
        }
    }
}
