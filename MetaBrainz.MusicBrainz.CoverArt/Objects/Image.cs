using System;
using System.Collections.Generic;

using MetaBrainz.Common.Json;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects;

internal sealed class Image : JsonBasedObject, IImage {

  public Image(int edit, string id, IThumbnails thumbnails, CoverArtType types) {
    this.Edit = edit;
    this.Id = id;
    this.Thumbnails = thumbnails;
    this.Types = types;
  }

  public bool Approved { get; init; }

  public bool Back { get; init; }

  public string? Comment { get; init; }

  public int Edit { get; }

  public bool Front { get; init; }

  public string Id { get; }

  public Uri? Location { get; init; }

  public IThumbnails Thumbnails { get; }

  public CoverArtType Types { get; }

  public IReadOnlyList<string>? UnknownTypes { get; init; }

}
