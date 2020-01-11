using System;
using System.Collections.Generic;

namespace MetaBrainz.MusicBrainz.CoverArt.Interfaces {

  /// <summary>A release on the CoverArt Archive.</summary>
  public interface IRelease : ICoverArtEntity {

    /// <summary>The images available for the release.</summary>
    IReadOnlyList<IImage> Images { get; }

    /// <summary>The URL on the MusicBrainz website where more information about the release can be found.</summary>
    Uri Location { get; }

  }

}
