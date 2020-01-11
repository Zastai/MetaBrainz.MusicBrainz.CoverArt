using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects {

  [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
  internal sealed class Image : IImage {

    [JsonPropertyName("approved")]
    public bool Approved { get; set; }

    [JsonPropertyName("back")]
    public bool Back { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; }

    [JsonPropertyName("edit")]
    public int Edit { get; set; }

    [JsonPropertyName("front")]
    public bool Front { get; set; }

    [JsonPropertyName("id"), JsonConverter(typeof(CovertArtArchiveIdConverter))]
    public string Id { get; set; }

    [JsonPropertyName("image")]
    public Uri Location { get; set; }

    public IThumbnails Thumbnails => this.TheThumbnails;

    [JsonPropertyName("thumbnails")]
    public Thumbnails TheThumbnails { get; set; }

    [JsonPropertyName("types")]
    public IReadOnlyList<string> TypeStrings { get; set; }

    public CoverArtType Types {
      get {
        if (this._types.HasValue)
          return this._types.Value;
        var types = CoverArtType.None;
        // Enum.Parse sadly can't be used, due to the "Raw/Unedited" type, which can't be mapped to an enum value.
        // So we just do the mapping ourselves here (and make it case insensitive while we're at it, not that that matters much).
        foreach (var type in this.TypeStrings) {
          switch (type.ToLowerInvariant()) {
            case "back":         types |= CoverArtType.Back;        break;
            case "booklet":      types |= CoverArtType.Booklet;     break;
            case "front":        types |= CoverArtType.Front;       break;
            case "liner":        types |= CoverArtType.Liner;       break;
            case "medium":       types |= CoverArtType.Medium;      break;
            case "obi":          types |= CoverArtType.Obi;         break;
            case "other":        types |= CoverArtType.Other;       break;
            case "poster":       types |= CoverArtType.Poster;      break;
            case "track":        types |= CoverArtType.Track;       break;
            case "raw/unedited": types |= CoverArtType.RawUnedited; break;
            case "spine":        types |= CoverArtType.Spine;       break;
            case "sticker":      types |= CoverArtType.Sticker;     break;
            case "tray":         types |= CoverArtType.Tray;        break;
            case "watermark":    types |= CoverArtType.Watermark;   break;
            default:
              // FIXME: Or should this throw an exception?
              Debug.Print($"+++ Encountered unknown CAA image type '{type}'.");
              break;
          }
        }
        this._types = types;
        return this._types.Value;
      }
    }

    private CoverArtType? _types;

    public IReadOnlyDictionary<string, object> UnhandledProperties => this.TheUnhandledProperties;

    [JsonExtensionData]
    public Dictionary<string, object> TheUnhandledProperties { get; set; }

  }

}
