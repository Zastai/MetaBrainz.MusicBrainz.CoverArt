using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>The raw bytes and accompanying content type for an image downloaded from the CoverArt Archive.</summary>
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "NotAccessedField.Global")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  public sealed class RawImage : IDisposable {

    internal RawImage(string type, MemoryStream data) {
      this.ContentType = type;
      this.Data        = data;
    }

    /// <summary>The content type for the image.</summary>
    public readonly string ContentType;

    /// <summary>The image's raw data.</summary>
    public readonly Stream Data;

    #if NETFX_TARGET // Only the .NET Framework has System.Drawing.Image

    /// <summary>Attempts to create an <see cref="System.Drawing.Image"/> from <see cref="Data"/>.</summary>
    /// <returns>A newly-constructed <see cref="System.Drawing.Image"/>.</returns>
    /// <remarks>This complete ignores <see cref="ContentType"/>.</remarks>
    /// <exception cref="ArgumentException">When the image data is not valid (or not supported by the <see cref="System.Drawing.Image"/> class).</exception>
    public System.Drawing.Image Decode(bool useEmbeddedColorManagement = false, bool validateImageData = false) {
      return System.Drawing.Image.FromStream(this.Data, useEmbeddedColorManagement, validateImageData);
    }

    #endif

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

    /// <summary>Finalizes this <see cref="RawImage"/>.</summary>
    ~RawImage() {
      this.Dispose(false);
    }

    #endregion

  }

}
