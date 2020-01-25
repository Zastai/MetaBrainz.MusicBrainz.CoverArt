using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace MetaBrainz.MusicBrainz.CoverArt.Interfaces {

  /// <summary>An image from the CoverArt Archive.</summary>
  [PublicAPI]
  public interface IImage : ICoverArtEntity {

    /// <summary>Flag indicating whether or not the image is approved.</summary>
    bool Approved { get; }

    /// <summary>Flag indicating whether or not this is the image marked as the main "back" image for a release.</summary>
    bool Back { get; }

    /// <summary>The comment attached to the image.</summary>
    string? Comment { get; }

    /// <summary>The MusicBrainz edit ID for the edit that initially added this image.</summary>
    /// <remarks>For more information about that edit, go to http://musicbrainz.org/edit/{edit-id}.</remarks>
    int Edit { get; }

    /// <summary>Flag indicating whether or not this is the image marked as the main "front" image for a release.</summary>
    bool Front { get; }

    /// <summary>The internal ID of the image. Can be used in a call to <see cref="CoverArt.FetchImage(Guid,string,CoverArtImageSize)"/>.</summary>
    /// <remarks>This ID is determined and set when the image is uploaded, and will never change.</remarks>
    string? Id { get; }

    /// <summary>URL at which the original uploaded image file can be found (in its original format).</summary>
    Uri? Location { get; }

    /// <summary>The thumbnails generated for the image.</summary>
    IThumbnails? Thumbnails { get; }

    /// <summary>The cover art type(s) matching this image, expressed as text.</summary>
    IReadOnlyList<string>? TypeStrings { get; }

    /// <summary>The cover art type(s) matching this image, expressed as an enumeration value.</summary>
    CoverArtType Types { get; }

  }

}
