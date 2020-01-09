﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json;

namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>Class representing a release on the CoverArt Archive.</summary>
  [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
  [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  [JsonObject(MemberSerialization.OptIn)]
  public class Release {

    /// <summary>The images available for the release.</summary>
    [JsonProperty("images")]
    public IReadOnlyList<Image> Images { get; private set; }

    /// <summary>The URL on the MusicBrainz website where more information about the release can be found.</summary>
    [JsonProperty("release")]
    public Uri Location { get; private set; }

  }

}
