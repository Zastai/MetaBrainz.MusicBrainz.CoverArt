using System;
using System.Collections.Generic;
using System.Text.Json;

using MetaBrainz.Common.Json;
using MetaBrainz.Common.Json.Converters;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;
using MetaBrainz.MusicBrainz.CoverArt.Objects;

namespace MetaBrainz.MusicBrainz.CoverArt.Json.Readers;

internal sealed class ReleaseReader : ObjectReader<Release> {

  public static readonly ReleaseReader Instance = new();

  protected override Release ReadObjectContents(ref Utf8JsonReader reader, JsonSerializerOptions options) {
    IReadOnlyList<IImage>? images = null;
    Uri? location = null;
    Dictionary<string, object?>? rest = null;
    while (reader.TokenType == JsonTokenType.PropertyName) {
      var prop = reader.GetPropertyName();
      try {
        reader.Read();
        switch (prop) {
          case "images":
            images = reader.ReadList(ImageReader.Instance, options);
            break;
          case "release":
            location = reader.GetUri();
            break;
          default:
            rest ??= [];
            rest[prop] = reader.GetOptionalObject(options);
            break;
        }
      }
      catch (Exception e) {
        throw new JsonException($"Failed to deserialize the '{prop}' property.", e);
      }
      reader.Read();
    }
    return new Release {
      Images = images ?? throw new JsonException("Required property 'images' missing or null."),
      Location = location ?? throw new JsonException("Required property 'release' missing or null."),
      UnhandledProperties = rest,
    };
  }

}
