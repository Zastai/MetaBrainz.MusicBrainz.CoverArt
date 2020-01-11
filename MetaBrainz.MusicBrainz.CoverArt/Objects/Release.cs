using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects {

  /// <summary>Class representing a release on the CoverArt Archive.</summary>
  [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
  internal sealed class Release : IRelease {

    public IReadOnlyList<IImage> Images => this.TheImages;

    /// <summary>The images available for the release.</summary>
    [JsonPropertyName("images")]
    public IReadOnlyList<Image> TheImages { get; set; }

    [JsonPropertyName("release")]
    public Uri Location { get; set; }

    public IReadOnlyDictionary<string, object> UnhandledProperties => this.TheUnhandledProperties;

    [JsonExtensionData]
    public Dictionary<string, object> TheUnhandledProperties { get; set; }

  }

}
