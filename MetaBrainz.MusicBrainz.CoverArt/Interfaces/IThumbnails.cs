using System;
using JetBrains.Annotations;

namespace MetaBrainz.MusicBrainz.CoverArt.Interfaces {

  /// <summary>The thumbnails images available for an image on the CovertArt Archive.</summary>
  [PublicAPI]
  public interface IThumbnails : ICoverArtEntity {

    /// <summary>The URI for the small thumbnail of the image, if available.</summary>
    Uri? Small { get; }

    /// <summary>The URI for the large thumbnail of the image, if available.</summary>
    Uri? Large { get; }

    /// <summary>The URI for the 250-pixel thumbnail of the image, if available.</summary>
    Uri? Size250 { get; }

    /// <summary>The URI for the 500-pixel thumbnail of the image, if available.</summary>
    Uri? Size500 { get; }

    /// <summary>The URI for the 1200-pixel "thumbnail" of the image, if available.</summary>
    Uri? Size1200 { get; }

  }

}
