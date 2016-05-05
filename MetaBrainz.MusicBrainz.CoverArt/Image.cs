using System;

namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>Information about an image from the CoverArt Archive.</summary>
  public sealed class Image {

    internal Image(JsonObjects.Image json) {
      this.Approved   = json.approved;
      this.Back       = json.back;
      this.Comment    = json.comment;
      this.Edit       = json.edit;
      this.Front      = json.front;
      this.Id         = json.id;
      this.Location   = json.image;
      this.Thumbnails = new Thumbnails(json.thumbnails);
      this.Types      = (CoverArtType) Enum.Parse(typeof(CoverArtType), string.Join(",", json.types), false);
    }

    /// <summary>Flag indicating whether or not the image is approved.</summary>
    public bool         Approved   { get; }

    /// <summary>Flag indicating whether or not this is the image marked as the main "back" image for a release.</summary>
    public bool         Back       { get; }

    /// <summary>The comment attached to the image.</summary>
    public string       Comment    { get; }

    /// <summary>The MusicBrainz edit ID for the edit that initially added this image.</summary>
    /// <remarks>For more information about that edit, got to http://musicbrainz.org/edit/{edit-id}.</remarks>
    public uint         Edit       { get; }

    /// <summary>Flag indicating whether or not this is the image marked as the main "front" image for a release.</summary>
    public bool         Front      { get; }

    /// <summary>The internal ID of the image. Can be used in a call to <see cref="CoverArt.FetchImage(Guid,string,ImageSize)"/>.</summary>
    /// <remarks>This ID is determined and set when the image is uploaded, and will never change.</remarks>
    public string       Id         { get; }

    /// <summary>URL at which the original uploaded image file can be found (in its original format).</summary>
    public Uri          Location   { get; }

    /// <summary>The thumbnails generated for the image.</summary>
    public Thumbnails   Thumbnails { get; }

    /// <summary>The cover art type(s) matching this image.</summary>
    public CoverArtType Types      { get; }

  }

}
