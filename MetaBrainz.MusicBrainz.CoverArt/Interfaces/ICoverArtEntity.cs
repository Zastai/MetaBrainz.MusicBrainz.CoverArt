using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MetaBrainz.MusicBrainz.CoverArt.Interfaces {

  /// <summary>Data from the CoverArt Archive.</summary>
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  public interface ICoverArtEntity {

    /// <summary>
    /// A dictionary containing all properties not handled by this library.<br/>
    /// This should be <see langword="null"/>; if it's not, please file a ticket, listing its contents.
    /// </summary>
    IReadOnlyDictionary<string, object> UnhandledProperties { get; }

  }

}