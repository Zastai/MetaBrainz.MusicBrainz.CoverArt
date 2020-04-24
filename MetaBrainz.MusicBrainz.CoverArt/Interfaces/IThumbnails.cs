using System;

using JetBrains.Annotations;

using MetaBrainz.Common.Json;

namespace MetaBrainz.MusicBrainz.CoverArt.Interfaces {

  /// <summary>The thumbnails images available for an image on the CovertArt Archive.</summary>
  [PublicAPI]
  public interface IThumbnails : IJsonBasedObject {

    /// <summary>The URI for the small thumbnail of the image, if available.</summary>
    /// <remarks>This field is deprecated and is equivalent to <see cref="Size250"/>.</remarks>
    Uri? Small { get; }

    /// <summary>The URI for the large thumbnail of the image, if available.</summary>
    /// <remarks>This field is deprecated and is equivalent to <see cref="Size500"/>.</remarks>
    Uri? Large { get; }

    /// <summary>The URI for the 250-pixel thumbnail of the image, if available.</summary>
    Uri? Size250 { get; }

    /// <summary>The URI for the 500-pixel thumbnail of the image, if available.</summary>
    Uri? Size500 { get; }

    /// <summary>The URI for the 1200-pixel "thumbnail" of the image, if available.</summary>
    Uri? Size1200 { get; }

  }

}
