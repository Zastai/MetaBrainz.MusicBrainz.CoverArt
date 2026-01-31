using System;
using System.Collections.Generic;

using MetaBrainz.Common.Json;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects;

/// <summary>Class representing a release on the CoverArt Archive.</summary>
internal sealed class Release : JsonBasedObject, IRelease {

  public required IReadOnlyList<IImage> Images { get; init; }

  public required Uri Location { get; init; }

}
