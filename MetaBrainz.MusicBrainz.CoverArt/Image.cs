using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json;

namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>Information about an image from the CoverArt Archive.</summary>
  [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
  [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  [JsonObject(MemberSerialization.OptIn)]
  public sealed class Image {

    /// <summary>Flag indicating whether or not the image is approved.</summary>
    [JsonProperty("approved")]
    public bool Approved { get; private set; }

    /// <summary>Flag indicating whether or not this is the image marked as the main "back" image for a release.</summary>
    [JsonProperty("back")]
    public bool Back { get; private set; }

    /// <summary>The comment attached to the image.</summary>
    [JsonProperty("comment")]
    public string Comment { get; private set; }

    /// <summary>The MusicBrainz edit ID for the edit that initially added this image.</summary>
    /// <remarks>For more information about that edit, got to http://musicbrainz.org/edit/{edit-id}.</remarks>
    [JsonProperty("edit")]
    public int Edit { get; private set; }

    /// <summary>Flag indicating whether or not this is the image marked as the main "front" image for a release.</summary>
    [JsonProperty("front")]
    public bool Front { get; private set; }

    /// <summary>The internal ID of the image. Can be used in a call to <see cref="CoverArt.FetchImage(Guid,string,ImageSize)"/>.</summary>
    /// <remarks>This ID is determined and set when the image is uploaded, and will never change.</remarks>
    [JsonProperty("id")]
    public string Id { get; private set; }

    /// <summary>URL at which the original uploaded image file can be found (in its original format).</summary>
    [JsonProperty("image")]
    public Uri Location { get; private set; }

    /// <summary>The thumbnails generated for the image.</summary>
    [JsonProperty("thumbnails")]
    public Thumbnails Thumbnails { get; private set; }

    /// <summary>The cover art type(s) matching this image, expressed as text.</summary>
    [JsonProperty("types")] 
    public IEnumerable<string> TypeStrings { get; private set; }

    /// <summary>The cover art type(s) matching this image, expressed as an enumeration value.</summary>
    public CoverArtType Types => this._types ?? (this._types = (CoverArtType) Enum.Parse(typeof(CoverArtType), string.Join(",", this.TypeStrings), false)).Value;

    private CoverArtType? _types;

  }

}
