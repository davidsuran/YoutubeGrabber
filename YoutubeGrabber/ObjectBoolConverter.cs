using System.Text.Json;
using System.Text.Json.Serialization;

namespace YoutubeGrabber
{
    public class ObjectBoolConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True)
            {
                return true;
            }

            if (reader.TokenType == JsonTokenType.False)
            {
                return false;
            }

            var converter = options.GetConverter(typeof(JsonElement)) as JsonConverter<JsonElement>;
            if (converter != null)
            {
                return converter.Read(ref reader, type, options);
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            throw new InvalidOperationException("Directly writing object not supported");
        }
    }
}
