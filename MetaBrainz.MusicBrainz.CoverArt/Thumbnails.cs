using System;

namespace MetaBrainz.MusicBrainz.CoverArtArchive {

  /// <summary>Class representing the thumbnails available for an <see cref="ImageInfo"/>.</summary>
  public sealed class Thumbnails {

    internal Thumbnails(JsonObjects.Thumbnails json) {
      this.Small = json.small;
      this.Large = json.large;
      this.Huge  = json.huge;
    }

    // Assumption: While thumbnail types may be added, it won't be an arbitrary number with arbitrary names.
    //             So, having specifically-named properties is nicer than a more generic dictionary-style interface.

    /// <summary>The URI for the small (250px) thumbnail of the image, if available.</summary>
    public Uri Small { get; }

    /// <summary>The URI for the large (500px) thumbnail of the image, if available.</summary>
    public Uri Large { get; }

    /// <summary>The URI for the huge (1200px) "thumbnail" of the image, if available.</summary>
    public Uri Huge { get; }

  }

}
