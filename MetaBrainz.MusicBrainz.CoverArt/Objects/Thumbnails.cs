using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects {

  [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
  internal sealed class Thumbnails : IThumbnails {

    [JsonPropertyName("small")]
    public Uri Small { get; set; }

    [JsonPropertyName("large")]
    public Uri Large { get; set; }

    [JsonPropertyName("250")]
    public Uri Size250 { get; set; }

    [JsonPropertyName("500")]
    public Uri Size500 { get; set; }

    [JsonPropertyName("1200")]
    public Uri Size1200 { get; set; }

    public IReadOnlyDictionary<string, object> UnhandledProperties => this.TheUnhandledProperties;

    [JsonExtensionData]
    public Dictionary<string, object> TheUnhandledProperties { get; set; }

  }

}
