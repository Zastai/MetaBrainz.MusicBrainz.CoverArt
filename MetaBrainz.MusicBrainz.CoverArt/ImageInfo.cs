using System;

namespace MetaBrainz.MusicBrainz.CoverArtArchive {

  public sealed class ImageInfo {

    internal ImageInfo(JsonObjects.Image json) {
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

    public bool         Approved   { get; }

    public bool         Back       { get; }

    public string       Comment    { get; }

    public uint         Edit       { get; }

    public bool         Front      { get; }

    public string       Id         { get; }

    public Uri          Location   { get; }

    public Thumbnails   Thumbnails { get; }

    public CoverArtType Types      { get; }

  }

}
