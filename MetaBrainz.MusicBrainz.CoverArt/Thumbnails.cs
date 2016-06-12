using System;
using System.Diagnostics.CodeAnalysis;

namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>Class representing the thumbnails available for an <see cref="Image"/>.</summary>
  public sealed class Thumbnails {

    /// <summary>The URI for the small (250px) thumbnail of the image, if available.</summary>
    public Uri Small { get; }

    /// <summary>The URI for the large (500px) thumbnail of the image, if available.</summary>
    public Uri Large { get; }

    /// <summary>The URI for the huge (1200px) "thumbnail" of the image, if available.</summary>
    public Uri Huge { get; }

    #region JSON-Based Construction

    internal Thumbnails(JSON json) {
      this.Small = json.small;
      this.Large = json.large;
      this.Huge  = json.huge;
    }

    // This class is created by a deserializer, so there's no point in complaining that its fields are uninitialized.
    #pragma warning disable 649

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal sealed class JSON {
      public Uri small;
      public Uri large;
      public Uri huge;
    }

    #endregion

  }

}
