using System;
using System.Collections.Generic;
using System.Text.Json;

using MetaBrainz.Common.Json;
using MetaBrainz.Common.Json.Converters;
using MetaBrainz.MusicBrainz.CoverArt.Objects;

namespace MetaBrainz.MusicBrainz.CoverArt.Json.Readers;

internal sealed class ThumbnailsReader : ObjectReader<Thumbnails> {

  public static readonly ThumbnailsReader Instance = new();

  protected override Thumbnails ReadObjectContents(ref Utf8JsonReader reader, JsonSerializerOptions options) {
    Uri? small = null;
    Uri? large = null;
    Uri? s250 = null;
    Uri? s500 = null;
    Uri? s1200 = null;
    Dictionary<string, object?>? rest = null;
    while (reader.TokenType == JsonTokenType.PropertyName) {
      var prop = reader.GetPropertyName();
      try {
        reader.Read();
        switch (prop) {
          case "small":
            small = reader.GetUri();
            break;
          case "large":
            large = reader.GetUri();
            break;
          case "250":
            s250 = reader.GetUri();
            break;
          case "500":
            s500 = reader.GetUri();
            break;
          case "1200":
            s1200 = reader.GetUri();
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
    return new Thumbnails {
      Large = large,
      Size250 = s250,
      Size500 = s500,
      Size1200 = s1200,
      Small = small,
      UnhandledProperties = rest,
    };
  }

}
