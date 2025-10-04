using System;
using System.Collections.Generic;
using System.Threading;

using JetBrains.Annotations;

using MetaBrainz.Common.Json;

namespace MetaBrainz.MusicBrainz.CoverArt.Interfaces;

/// <summary>An image from the CoverArt Archive.</summary>
[PublicAPI]
public interface IImage : IJsonBasedObject {

  /// <summary>Flag indicating whether the image is approved.</summary>
  bool Approved { get; }

  /// <summary>Flag indicating whether this is the image marked as the main "back" image for a release.</summary>
  bool Back { get; }

  /// <summary>The comment attached to the image, if any.</summary>
  string? Comment { get; }

  /// <summary>The MusicBrainz edit ID for the edit that initially added this image.</summary>
  /// <remarks>For more information about that edit, go to http://musicbrainz.org/edit/{edit-id}.</remarks>
  int Edit { get; }

  /// <summary>Flag indicating whether this is the image marked as the main "front" image for a release.</summary>
  bool Front { get; }

  /// <summary>
  /// The internal ID of the image.
  /// This can be used in a call to <see cref="CoverArt.FetchImageAsync(Guid,string,CoverArtImageSize,CancellationToken)"/>.
  /// </summary>
  /// <remarks>This ID is determined and set when the image is uploaded, and will never change.</remarks>
  string Id { get; }

  /// <summary>URL at which the original uploaded image file can be found (in its original format).</summary>
  Uri? Location { get; }

  /// <summary>The thumbnails generated for the image.</summary>
  IThumbnails Thumbnails { get; }

  /// <summary>The cover art type(s) matching this image.</summary>
  CoverArtType Types { get; }

  /// <summary>
  /// The unknown cover art type(s) matching this image.
  /// This will be <see langword="null"/> unless <see cref="Types"/> includes <see cref="CoverArtType.Unknown"/>.
  /// </summary>
  IReadOnlyList<string>? UnknownTypes { get; }

}
