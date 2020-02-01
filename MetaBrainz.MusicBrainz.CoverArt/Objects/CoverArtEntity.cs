using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects {

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  internal abstract class CoverArtEntity : ICoverArtEntity {

    public IReadOnlyDictionary<string, object?>? UnhandledProperties
      => this._unhandled ??= JsonUtils.Unwrap(this.TheUnhandledProperties);

    private Dictionary<string, object?>? _unhandled;

    [JsonExtensionData]
    public Dictionary<string, object?>? TheUnhandledProperties { get; set; }

  }

}
