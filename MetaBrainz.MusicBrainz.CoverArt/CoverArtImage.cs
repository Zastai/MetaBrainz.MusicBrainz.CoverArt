using System;
using System.Drawing;
using System.IO;

using JetBrains.Annotations;

namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>The raw bytes and accompanying content type for an image downloaded from the CoverArt Archive.</summary>
  [PublicAPI]
  public sealed class CoverArtImage : IDisposable {

    internal CoverArtImage(string id, CoverArtImageSize size, string type, Stream data) {
      this.Id = id;
      this.Size = size;
      this.ContentType = type;
      this.Data = data;
    }

    /// <summary>The image's unique ID.</summary>
    public readonly string Id;

    /// <summary>The image's size.</summary>
    public readonly CoverArtImageSize Size;

    /// <summary>The content type for the image.</summary>
    public readonly string ContentType;

    /// <summary>The image's raw data.</summary>
    public readonly Stream Data;

    /// <summary>Attempts to create an <see cref="System.Drawing.Image"/> from <see cref="Data"/>.</summary>
    /// <returns>A newly-constructed <see cref="System.Drawing.Image"/>.</returns>
    /// <remarks>This complete ignores <see cref="ContentType"/>.</remarks>
    /// <exception cref="ArgumentException">
    /// When the image data is not valid (or not supported by the <see cref="System.Drawing.Image"/> class).
    /// </exception>
    public Image Decode(bool useEmbeddedColorManagement = false, bool validateImageData = false) {
      return Image.FromStream(this.Data, useEmbeddedColorManagement, validateImageData);
    }

    #region IDisposable

    private bool _disposed;

    private void Dispose(bool disposing) {
      if (this._disposed)
        return;
      if (disposing)
        this.Data.Dispose();
      this._disposed = true;
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose() {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>Finalizes this <see cref="CoverArtImage"/>.</summary>
    ~CoverArtImage() {
      this.Dispose(false);
    }

    #endregion

  }

}
