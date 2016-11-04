using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>Class representing a release on the CoverArt Archive.</summary>
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
  public class Release {

    /// <summary>The images available for the release.</summary>
    public ReadOnlyCollection<Image> Images { get; }

    /// <summary>The URL on the MusicBrainz website where more information about the release can be found.</summary>
    public Uri Location { get; }

    #region JSON-Based Construction

    internal Release(JSON json) {
      this.Location = json.release;
      var images = new List<Image>(json.images.Length);
      foreach (var img in json.images)
        images.Add(new Image(img));
      this.Images = images.AsReadOnly();
    }

    // This class is created by a deserializer, so there's no point in complaining that its fields are uninitialized.
    #pragma warning disable 649

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal sealed class JSON {
      public Uri          release;
      public Image.JSON[] images;
    }

    #endregion

  }

}
