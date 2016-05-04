using System;

// These are all created by the deserializer, so there's no point in complaining that their fields are uninitialized.
#pragma warning disable 649

namespace MetaBrainz.MusicBrainz.CoverArtArchive {

  /// <summary>Class encapsulating the "raw" JSON objects as produced by the deserializer.</summary>
  internal static class JsonObjects {

    public sealed class Release {
      public Uri     release;
      public Image[] images;
    }

    public sealed class Image {
      public string     id;
      public Uri        image;
      public bool       back;
      public Thumbnails thumbnails;
      public string[]   types;
      public string     comment;
      public uint       edit;
      public bool       front;
      public bool       approved;
    }

    public sealed class Thumbnails {
      public Uri small;
      public Uri large;
      public Uri huge;
    }

  }

}
