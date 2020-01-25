using System;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects {

  [UsedImplicitly]
  internal sealed class Thumbnails : CoverArtEntity, IThumbnails {

    [JsonPropertyName("small")]
    [UsedImplicitly]
    public Uri? Small { get; set; }

    [JsonPropertyName("large")]
    [UsedImplicitly]
    public Uri? Large { get; set; }

    [JsonPropertyName("250")]
    [UsedImplicitly]
    public Uri? Size250 { get; set; }

    [JsonPropertyName("500")]
    [UsedImplicitly]
    public Uri? Size500 { get; set; }

    [JsonPropertyName("1200")]
    [UsedImplicitly]
    public Uri? Size1200 { get; set; }

  }

}
