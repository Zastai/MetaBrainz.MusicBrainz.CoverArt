using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

using MetaBrainz.Common.Json;
using MetaBrainz.Common.Json.Converters;
using MetaBrainz.MusicBrainz.CoverArt.Objects;

namespace MetaBrainz.MusicBrainz.CoverArt.Json.Readers;

internal sealed class ImageReader : ObjectReader<Image> {

  public static readonly ImageReader Instance = new ImageReader();

  protected override Image ReadObjectContents(ref Utf8JsonReader reader, JsonSerializerOptions options) {
    var approved = false;
    var back = false;
    string? comment = null;
    int? edit = null;
    var front = false;
    string? id = null;
    Uri? image = null;
    Thumbnails? thumbnails = null;
    var types = CoverArtType.None;
    List<string>? unknownTypes = null;
    Dictionary<string, object?>? rest = null;
    while (reader.TokenType == JsonTokenType.PropertyName) {
      var prop = reader.GetPropertyName();
      try {
        reader.Read();
        switch (prop) {
          case "approved":
            approved = reader.GetBoolean();
            break;
          case "back":
            back = reader.GetBoolean();
            break;
          case "comment":
            comment = reader.GetString();
            break;
          case "edit":
            edit = reader.GetInt32();
            break;
          case "front":
            front = reader.GetBoolean();
            break;
          case "image":
            image = reader.GetUri();
            break;
          case "id":
            if (reader.TokenType == JsonTokenType.String) {
              id = reader.GetString();
            }
            else if (reader.TryGetUInt64(out var numeric)) {
              id = numeric.ToString(CultureInfo.InvariantCulture);
            }
            else {
              throw new JsonException($"A CoverArt Archive ID is expected to be expressed either as a number or a string, but a '{reader.TokenType}' token was found instead.");
            }
            break;
          case "thumbnails":
            thumbnails = reader.GetObject(ThumbnailsReader.Instance, options);
            break;
          case "types": {
            foreach (var type in reader.ReadList<string>(options) ?? Enumerable.Empty<string>()) {
              ImageReader.AddCoverArtType(type, ref types, ref unknownTypes);
            }
            break;
          }
          default:
            rest ??= new Dictionary<string, object?>();
            rest[prop] = reader.GetOptionalObject(options);
            break;
        }
      }
      catch (Exception e) {
        throw new JsonException($"Failed to deserialize the '{prop}' property.", e);
      }
      reader.Read();
    }
    if (!edit.HasValue) {
      throw new JsonException("Required property 'edit' missing or null.");
    }
    if (id == null) {
      throw new JsonException("Required property 'id' missing or null.");
    }
    if (thumbnails == null) {
      throw new JsonException("Required property 'thumbnails' missing or null.");
    }
    return new Image(edit.Value, id, thumbnails, types) {
      Approved = approved,
      Back = back,
      Comment = comment,
      Front = front,
      Location = image,
      UnknownTypes = unknownTypes,
      UnhandledProperties = rest
    };
  }

  private static void AddCoverArtType(string type, ref CoverArtType types, ref List<string>? unknownTypes) {
    switch (type) {
      case "Back":
        types |= CoverArtType.Back;
        break;
      case "Booklet":
        types |= CoverArtType.Booklet;
        break;
      case "Front":
        types |= CoverArtType.Front;
        break;
      case "Liner":
        types |= CoverArtType.Liner;
        break;
      case "Medium":
        types |= CoverArtType.Medium;
        break;
      case "Obi":
        types |= CoverArtType.Obi;
        break;
      case "Other":
        types |= CoverArtType.Other;
        break;
      case "Poster":
        types |= CoverArtType.Poster;
        break;
      case "Track":
        types |= CoverArtType.Track;
        break;
      case "Raw/Unedited":
        types |= CoverArtType.RawUnedited;
        break;
      case "Spine":
        types |= CoverArtType.Spine;
        break;
      case "Sticker":
        types |= CoverArtType.Sticker;
        break;
      case "Tray":
        types |= CoverArtType.Tray;
        break;
      case "Watermark":
        types |= CoverArtType.Watermark;
        break;
      default:
        types |= CoverArtType.Unknown;
        unknownTypes ??= new List<string>();
        unknownTypes.Add(type);
        break;
    }
  }

}
