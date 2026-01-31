using System;
using System.Collections.Generic;

using MetaBrainz.Common.Json;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects;

internal sealed class Image : JsonBasedObject, IImage {

  public bool Approved { get; init; }

  public bool Back { get; init; }

  public string? Comment { get; init; }

  public int Edit { get; init; }

  public bool Front { get; init; }

  public required string Id { get; init; }

  public Uri? Location { get; init; }

  public required IThumbnails Thumbnails { get; init; }

  public required CoverArtType Types { get; init; }

  public IReadOnlyList<string>? UnknownTypes { get; init; }

}
