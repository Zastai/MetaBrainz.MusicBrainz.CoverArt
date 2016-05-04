using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MetaBrainz.MusicBrainz.CoverArtArchive {

  public class ReleaseInfo {

    internal ReleaseInfo(JsonObjects.Release json) {
      this.Location = json.release;
      this._images = new List<ImageInfo>(json.images.Length);
      foreach (var img in json.images)
        this._images.Add(new ImageInfo(img));
    }

    public Uri Location;

    private readonly List<ImageInfo> _images;

    public ReadOnlyCollection<ImageInfo> Images => this._images.AsReadOnly();

  }

}
