using System;
using System.Drawing;
using System.IO;

namespace MetaBrainz.MusicBrainz.CoverArtArchive {

  /// <summary>The raw bytes and accompanying content type for an image downloaded from the CoverArt Archive.</summary>
  public sealed class RawImage : IDisposable {

    internal RawImage(string type, byte[] data) {
      this.ContentType = type;
      this.Data        = new MemoryStream(data, false);
    }

    /// <summary>The content type for the image.</summary>
    public readonly string ContentType;

    /// <summary>The image's raw data.</summary>
    public readonly Stream Data;

    /// <summary>Attempts to create an <see cref="Image"/> from <see cref="Data"/>.</summary>
    /// <returns>A newly-constructed <see cref="Image"/>.</returns>
    /// <remarks>This complete ignores <see cref="ContentType"/>.</remarks>
    /// <exception cref="ArgumentException">When the image data is not valid (or not supported by the <see cref="Image"/> class).</exception>
    public Image Decode(bool useEmbeddedColorManagement = false, bool validateImageData = false) {
      return Image.FromStream(this.Data, useEmbeddedColorManagement, validateImageData);
    }

    #region IDisposable

    private bool disposed = false;

    private void Dispose(bool disposing) {
      if (this.disposed)
        return;
      if (disposing)
        this.Data.Dispose();
      this.disposed = true;
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
