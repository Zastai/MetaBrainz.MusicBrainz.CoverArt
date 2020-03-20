using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using JetBrains.Annotations;

using MetaBrainz.Common.Json;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects {

  /// <summary>Class representing a release on the CoverArt Archive.</summary>
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  internal sealed class Release : JsonBasedObject, IRelease {

    [JsonPropertyName("images")]
    public IReadOnlyList<IImage>? Images { get; set; }

    [JsonPropertyName("release")]
    public Uri? Location { get; set; }

  }

}
