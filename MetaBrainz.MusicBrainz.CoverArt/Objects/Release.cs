using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using JetBrains.Annotations;

using MetaBrainz.Common.Json;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects {

  /// <summary>Class representing a release on the CoverArt Archive.</summary>
  internal sealed class Release : JsonBasedObject, IRelease {

    public Release(IReadOnlyList<IImage> images, Uri location) {
      this.Images = images;
      this.Location = location;
    }

    public IReadOnlyList<IImage> Images { get; }

    public Uri Location { get; }

  }

}
