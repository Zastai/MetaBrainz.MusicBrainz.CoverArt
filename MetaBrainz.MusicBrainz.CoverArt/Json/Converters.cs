using System.Collections.Generic;
using System.Text.Json.Serialization;

using MetaBrainz.MusicBrainz.CoverArt.Json.Readers;

namespace MetaBrainz.MusicBrainz.CoverArt.Json {

  internal static class Converters {

    public static IEnumerable<JsonConverter> Readers {
      get {
        yield return ReleaseReader.Instance;
      }
    }

  }

}
