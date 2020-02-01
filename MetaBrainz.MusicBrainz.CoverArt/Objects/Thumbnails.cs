using System;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects {

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  internal sealed class Thumbnails : CoverArtEntity, IThumbnails {

    [JsonPropertyName("small")]
    public Uri? Small { get; set; }

    [JsonPropertyName("large")]
    public Uri? Large { get; set; }

    [JsonPropertyName("250")]
    public Uri? Size250 { get; set; }

    [JsonPropertyName("500")]
    public Uri? Size500 { get; set; }

    [JsonPropertyName("1200")]
    public Uri? Size1200 { get; set; }

  }

}
