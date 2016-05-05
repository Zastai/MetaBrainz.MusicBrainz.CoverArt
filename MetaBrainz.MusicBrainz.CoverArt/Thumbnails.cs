using System;

namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>Class representing the thumbnails available for an <see cref="Image"/>.</summary>
  public sealed class Thumbnails {

    internal Thumbnails(JsonObjects.Thumbnails json) {
      this.Small = json.small;
      this.Large = json.large;
      this.Huge  = json.huge;
    }

    /// <summary>The URI for the small (250px) thumbnail of the image, if available.</summary>
    public Uri Small { get; }

    /// <summary>The URI for the large (500px) thumbnail of the image, if available.</summary>
    public Uri Large { get; }

    /// <summary>The URI for the huge (1200px) "thumbnail" of the image, if available.</summary>
    public Uri Huge { get; }

  }

}
