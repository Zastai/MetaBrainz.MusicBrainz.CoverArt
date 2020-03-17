using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects {

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  internal abstract class CoverArtEntity : ICoverArtEntity {

    IReadOnlyDictionary<string, object?>? ICoverArtEntity.UnhandledProperties => this.UnhandledProperties;

    [JsonExtensionData]
    public Dictionary<string, object?>? UnhandledProperties { get; set; }

  }

}
