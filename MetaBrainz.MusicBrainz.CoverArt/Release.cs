using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>Class representing a release on the CoverArt Archive.</summary>
  public class Release {

    private readonly List<Image> _images;

    /// <summary>The URL on the MusicBrainz website where more information about the release can be found.</summary>
    public Uri Location;

    /// <summary>The images available for the release.</summary>
    public ReadOnlyCollection<Image> Images => this._images.AsReadOnly();

    #region JSON-Based Construction

    internal Release(JSON json) {
      this.Location = json.release;
      this._images = new List<Image>(json.images.Length);
      foreach (var img in json.images)
        this._images.Add(new Image(img));
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
