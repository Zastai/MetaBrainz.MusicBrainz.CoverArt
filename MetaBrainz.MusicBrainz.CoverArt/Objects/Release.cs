using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects {

  /// <summary>Class representing a release on the CoverArt Archive.</summary>
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  internal sealed class Release : CoverArtEntity, IRelease {

    public IReadOnlyList<IImage>? Images => this.TheImages;

    /// <summary>The images available for the release.</summary>
    [JsonPropertyName("images")]
    public IReadOnlyList<Image>? TheImages { get; set; }

    [JsonPropertyName("release")]
    public Uri? Location { get; set; }

  }

}
