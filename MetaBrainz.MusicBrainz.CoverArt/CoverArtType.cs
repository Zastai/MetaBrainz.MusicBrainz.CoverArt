using System;
using System.Diagnostics.CodeAnalysis;

namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>Flag enumeration of the supported image types.</summary>
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  [Flags]
  public enum CoverArtType : long {

    /// <summary>No cover art type has been set.</summary>
    None        = 0,

    /// <summary>The album cover, this is the front of the packaging of an audio recording (or in the case of a digital release the image associated with it in a digital media store).</summary>
    Front       = 1L <<  0,

    /// <summary>The back of the package of an audio recording, this will often contain the track listing, barcode and copyright information.</summary>
    Back        = 1L <<  1,

    /// <summary>A small book or group of pages inserted into the compact disc or DVD jewel case or the equivalent packaging for vinyl records and cassettes. Digital releases sometimes include a booklet in a digital file (usually PDF). Booklets often contain liner notes, song lyrics and/or photographs of the artist or band.</summary>
    Booklet     = 1L <<  2,

    /// <summary>The medium contains the audio recording, for a compact disc release it is the compact disc itself, similarly for a vinyl release it is the vinyl disc itself, etc.</summary>
    Medium      = 1L <<  3,

    /// <summary>The image behind or on the tray containing the medium. For jewel cases, this is usually printed on the other side of the piece of paper with the back image.</summary>
    Tray        = 1L <<  4,

    /// <summary>An obi is a strip of paper around the spine (or occasionally one of the other edges of the packaging).</summary>
    Obi         = 1L <<  5,

    /// <summary>A spine is the edge of the package of an audio recording, it is often the only part visible when recordings are stacked or stored in a shelf. For compact discs the spine is usually part of the back cover scan, and should not be uploaded separately.</summary>
    Spine       = 1L <<  6,

    /// <summary>Digital releases sometimes have cover art associated with each individual track of a release (typically embedded in the .mp3 files), use this type for images associated with individual tracks.</summary>
    Track       = 1L <<  7,

    /// <summary>A liner is a protective sleeve surrounding a medium (usually a vinyl record, but sometimes a CD), often printed with notes or images.</summary>
    Liner       = 1L <<  8,

    /// <summary>A sticker is an adhesive piece of paper, that is attached to the plastic film or enclosed inside the packaging.</summary>
    Sticker     = 1L <<  9,

    /// <summary>A poster included with a release. May be the same size as the packaging or larger (in this case it would fold out). Such posters are often printed on the back of a fold-out booklet but are sometimes bundled separately.</summary>
    Poster      = 1L << 10,

    /// <summary>A watermark is a piece of text or an image which is not part of the cover art but is added by the person who scanned the cover art. Images without any watermarks are preferred where possible - this type is useful in cases where either the only available image is watermarked, or where a better quality watermarked image is uploaded alongside a poorer quality non-watermarked image.</summary>
    Watermark   = 1L << 11,

    /// <summary>An image that is usable for reference, but needs more work to be usable for tagging (for example, an uncropped scan).</summary>
    RawUnedited = 1L << 12,

    /// <summary>Anything which doesn't fit in any of the other types.</summary>
    Other       = 1L << 63,

  }

}
