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

    /// <summary>The URI for the small (250px) thumbnail of the image, if available.</summary>
    [JsonProperty("small")]
    public Uri Small { get; private set; }

    /// <summary>The URI for the large (500px) thumbnail of the image, if available.</summary>
    [JsonProperty("large")]
    public Uri Large { get; private set; }

    /// <summary>The URI for the huge (1200px) "thumbnail" of the image, if available.</summary>
    [JsonProperty("huge")]
    public Uri Huge { get; private set; }

  }

}
