using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>Class representing a release on the CoverArt Archive.</summary>
  public class Release {

    internal Release(JsonObjects.Release json) {
      this.Location = json.release;
      this._images = new List<Image>(json.images.Length);
      foreach (var img in json.images)
        this._images.Add(new Image(img));
    }

    private readonly List<Image> _images;

    /// <summary>The URL on the MusicBrainz website where more information about the release can be found.</summary>
    public Uri Location;

    /// <summary>The images available for the release.</summary>
    public ReadOnlyCollection<Image> Images => this._images.AsReadOnly();

  }

}
