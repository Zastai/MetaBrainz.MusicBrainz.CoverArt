using System;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json;

namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>Class representing the thumbnails available for an <see cref="Image"/>.</summary>
  [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
  [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  [JsonObject(MemberSerialization.OptIn)]
  public sealed class Thumbnails {

    /// <summary>The URI for the small thumbnail of the image, if available.</summary>
    [JsonProperty("small")]
    public Uri Small { get; private set; }

    /// <summary>The URI for the large thumbnail of the image, if available.</summary>
    [JsonProperty("large")]
    public Uri Large { get; private set; }

    /// <summary>The URI for the 250-pixel thumbnail of the image, if available.</summary>
    [JsonProperty("250")]
    public Uri Size250 { get; private set; }

    /// <summary>The URI for the 500-pixel thumbnail of the image, if available.</summary>
    [JsonProperty("500")]
    public Uri Size500 { get; private set; }

    /// <summary>The URI for the 1200-pixel "thumbnail" of the image, if available.</summary>
    [JsonProperty("1200")]
    public Uri Size1200 { get; private set; }

  }

}
