#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpGen.Doc;
using static SharpGenTools.Sdk.Documentation.DocConverterUtilities;

namespace SharpGenTools.Sdk.Documentation
{
    internal sealed class DocItemConverter : JsonConverter<IDocItem>
    {
        public override bool CanConvert(Type typeToConvert) => typeof(IDocItem).IsAssignableFrom(typeToConvert);

        public override IDocItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var item = new DocItem();

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            item.ShortId = Read<string>(ref reader, nameof(IDocItem.ShortId));
            AssignList(item.Names, Read<List<string>>(ref reader, nameof(IDocItem.Names)));
            item.Summary = Read<string>(ref reader, nameof(IDocItem.Summary));
            item.Remarks = Read<string>(ref reader, nameof(IDocItem.Remarks));
            item.Return = Read<string>(ref reader, nameof(IDocItem.Return));
            AssignList(item.Items, Read<List<IDocSubItem>>(ref reader, nameof(IDocItem.Items)));
            AssignList(item.SeeAlso, Read<List<string>>(ref reader, nameof(IDocItem.SeeAlso)));

            if (!reader.Read())
                throw new JsonException();

            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException();

            item.IsDirty = false;

            return item;

            T Read<T>(ref Utf8JsonReader reader, string expectedPropertyName) =>
                ReadProperty<T>(ref reader, expectedPropertyName, options);
        }

        public override void Write(Utf8JsonWriter writer, IDocItem value, JsonSerializerOptions options)
        {
            JsonConverter<IList<string>>? listStringConverter = null;
            JsonConverter<IList<IDocSubItem>>? listSubItemConverter = null;

            writer.WriteStartObject();

            writer.WriteString(nameof(value.ShortId), value.ShortId);
            Write(ref listStringConverter, nameof(value.Names), value.Names);
            writer.WriteString(nameof(value.Summary), value.Summary);
            writer.WriteString(nameof(value.Remarks), value.Remarks);
            writer.WriteString(nameof(value.Return), value.Return);
            Write(ref listSubItemConverter, nameof(value.Items), value.Items);
            Write(ref listStringConverter, nameof(value.SeeAlso), value.SeeAlso);

            writer.WriteEndObject();

            value.IsDirty = false;

            void Write<T>(ref JsonConverter<T>? converter, string name, T value) =>
                WriteProperty(writer, options, ref converter, name, value);
        }
    }
}