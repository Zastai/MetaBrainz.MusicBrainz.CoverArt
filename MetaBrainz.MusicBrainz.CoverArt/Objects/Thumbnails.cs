using System;
using System.Text.Json.Serialization;

using JetBrains.Annotations;

using MetaBrainz.Common.Json;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;

namespace MetaBrainz.MusicBrainz.CoverArt.Objects {

  internal sealed class Thumbnails : JsonBasedObject, IThumbnails {

    public Uri? Small { get; set; }

    public Uri? Large { get; set; }

    public Uri? Size250 { get; set; }

    public Uri? Size500 { get; set; }

    public Uri? Size1200 { get; set; }

  }

}
