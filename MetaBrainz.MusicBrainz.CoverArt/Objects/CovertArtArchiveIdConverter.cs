using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects {

  /// <summary>
  /// A JSON converter for a CoverArt Archive ID value (which is sometimes written as a number and sometimes as a string).
  /// </summary>
  internal sealed class CovertArtArchiveIdConverter : JsonConverter<string> {

    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      if (reader.TokenType == JsonTokenType.String)
        return reader.GetString();
      if (reader.TryGetUInt64(out var numeric))
        return numeric.ToString(CultureInfo.InvariantCulture);
      throw new JsonException($"A CoverArt Archive ID is expected to be expressed either as a number or a string, but a '{reader.TokenType}' token was found instead.");
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) {
      writer.WriteStringValue(value);
    }

  }

}
