namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>Enumeration indicating the desired size of an image to be fetched from the CoverArt Archive.</summary>
  public enum ImageSize : ushort {

    /// <summary>The original image as uploaded. This can be of any size or media type.</summary>
    Original = 0,

    /// <summary>The small (250px) thumbnail of the image. Will always be image/jpeg.</summary>
    SmallThumbnail = 250,

    /// <summary>The large (500px) thumbnail of the image. Will always be image/jpeg.</summary>
    LargeThumbnail = 500,

    /// <summary>The huge (1200px) thumbnail of the image. Will always be image/jpeg.</summary>
    HugeThumbnail = 1200,

  }

}
