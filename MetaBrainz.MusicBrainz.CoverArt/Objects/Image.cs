using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

using JetBrains.Annotations;

using MetaBrainz.Common.Json;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects {

  internal sealed class Image : JsonBasedObject, IImage {

    public Image(int edit, string id, IThumbnails thumbnails, CoverArtType types) {
      this.Edit = edit;
      this.Id = id;
      this.Thumbnails = thumbnails;
      this.Types = types;
    }

    public bool Approved { get; set; }

    public bool Back { get; set; }

    public string? Comment { get; set; }

    public int Edit { get; }

    public bool Front { get; set; }

    public string Id { get; }

    public Uri? Location { get; set; }

    public IThumbnails Thumbnails { get; }

    public CoverArtType Types { get; }

    public IReadOnlyList<string>? UnknownTypes { get; set; }

  }

}
