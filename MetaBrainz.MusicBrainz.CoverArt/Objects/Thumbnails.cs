using System;

using MetaBrainz.Common.Json;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects;

internal sealed class Thumbnails : JsonBasedObject, IThumbnails {

  public Uri? Small { get; init; }

  public Uri? Large { get; init; }

  public Uri? Size250 { get; init; }

  public Uri? Size500 { get; init; }

  public Uri? Size1200 { get; init; }

}
