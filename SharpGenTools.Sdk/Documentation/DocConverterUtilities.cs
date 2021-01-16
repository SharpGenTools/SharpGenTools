using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpGen.Doc;
using SharpGenTools.Sdk.Internal;

namespace SharpGenTools.Sdk.Documentation
{
    internal static class DocConverterUtilities
    {
        public static T ReadProperty<T>(ref Utf8JsonReader reader, string expectedPropertyName,
                                        JsonSerializerOptions options)
        {
            if (!reader.Read())
                throw new JsonException();

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            var propertyName = reader.GetString();

            if (!reader.Read())
                throw new JsonException();

            if (propertyName != expectedPropertyName)
                throw new JsonException();

            return JsonSerializer.Deserialize<T>(ref reader, options);
        }

        public static void WriteProperty<T>(Utf8JsonWriter writer, JsonSerializerOptions options,
                                            ref JsonConverter<T> converter,
                                            string name, T value)
        {
            converter ??= (JsonConverter<T>) options.GetConverter(typeof(T));

            writer.WritePropertyName(name);
            converter.Write(writer, value, options);
        }

        public static void AssignList<T>(IList<T> storage, IEnumerable<T> source)
        {
            storage.Clear();
            foreach (var subItem in source)
                storage.Add(subItem);
        }
    }
}