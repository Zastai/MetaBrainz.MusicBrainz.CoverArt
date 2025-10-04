using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using MetaBrainz.Common;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;
using MetaBrainz.MusicBrainz.CoverArt.Objects;

namespace MetaBrainz.MusicBrainz.CoverArt;

public sealed partial class CoverArt {

  /// <summary>Fetch the main "back" image for the specified release.</summary>
  /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
  /// <param name="size">
  /// The requested image size; <see cref="CoverArtImageSize.Original"/> can be any content type, but the thumbnails are always
  /// JPEG.
  /// </param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>An asynchronous operation returning the requested image data.</returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP error status; the most common cases will be:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no "back" image set);
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  /// <exception cref="HttpRequestException">
  /// When the request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or
  /// timeout.
  /// </exception>
  /// <exception cref="TaskCanceledException">
  /// When the request failed due to timeout (.NET Core and .NET 5 and later only).
  /// </exception>
  public Task<CoverArtImage> FetchBackAsync(Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original,
                                            CancellationToken cancellationToken = new())
    => this.FetchImageAsync("release", mbid, "back", size, cancellationToken);

  /// <summary>Fetch the main "front" image for the specified release, in the specified size.</summary>
  /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
  /// <param name="size">
  /// The requested image size; <see cref="CoverArtImageSize.Original"/> can be any content type, but the thumbnails are always
  /// JPEG.
  /// </param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>An asynchronous operation returning the requested image data.</returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP error status; the most common cases will be:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no "front" image set);
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  /// <exception cref="HttpRequestException">
  /// When the request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or
  /// timeout.
  /// </exception>
  /// <exception cref="TaskCanceledException">
  /// When the request failed due to timeout (.NET Core and .NET 5 and later only).
  /// </exception>
  public Task<CoverArtImage> FetchFrontAsync(Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original,
                                             CancellationToken cancellationToken = new())
    => this.FetchImageAsync("release", mbid, "front", size, cancellationToken);

  /// <summary>Fetch the main "front" image for the specified release group, in the specified size.</summary>
  /// <param name="mbid">The MusicBrainz release group ID for which the image is requested.</param>
  /// <param name="size">
  /// The requested image size; <see cref="CoverArtImageSize.Original"/> can be any content type, but the thumbnails are always
  /// JPEG.
  /// </param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>An asynchronous operation returning the requested image data.</returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP error status; the most common cases will be:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release group does not exist (or has no "front" image set);
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  /// <exception cref="HttpRequestException">
  /// When the request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or
  /// timeout.
  /// </exception>
  /// <exception cref="TaskCanceledException">
  /// When the request failed due to timeout (.NET Core and .NET 5 and later only).
  /// </exception>
  public Task<CoverArtImage> FetchGroupFrontAsync(Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original,
                                                  CancellationToken cancellationToken = new())
    => this.FetchImageAsync("release-group", mbid, "front", size, cancellationToken);

  /// <summary>Fetch information about the cover art associated with the specified MusicBrainz release group (if any).</summary>
  /// <param name="mbid">The MusicBrainz release group ID for which cover art information is requested.</param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>
  /// An asynchronous operation returning an <see cref="IRelease"/> object containing information about the cover art for the
  /// release group's main release.
  /// </returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP error status; the most common cases will be:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release group does not exist (or has no associated cover art);
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  /// <exception cref="HttpRequestException">
  /// When the request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or
  /// timeout.
  /// </exception>
  /// <exception cref="TaskCanceledException">
  /// When the request failed due to timeout (.NET Core and .NET 5 and later only).
  /// </exception>
  public Task<IRelease> FetchGroupReleaseAsync(Guid mbid, CancellationToken cancellationToken = new())
    => this.FetchReleaseAsync("release-group", mbid, cancellationToken);

  /// <summary>Fetch information about the cover art associated with the specified MusicBrainz release group (if any).</summary>
  /// <param name="mbid">The MusicBrainz release group ID for which cover art information is requested.</param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>
  /// An asynchronous operation returning an <see cref="IRelease"/> object containing information about the cover art for the
  /// release group's main release, or <see langword="null"/> if the release group does not exist (or has no associated cover art).
  /// </returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP error status; the most common case will be status 503
  /// (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </exception>
  /// <exception cref="HttpRequestException">
  /// When the request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or
  /// timeout.
  /// </exception>
  /// <exception cref="TaskCanceledException">
  /// When the request failed due to timeout (.NET Core and .NET 5 and later only).
  /// </exception>
  public Task<IRelease?> FetchGroupReleaseIfAvailableAsync(Guid mbid, CancellationToken cancellationToken = new())
    => this.FetchReleaseIfAvailableAsync("release-group", mbid, cancellationToken);

  /// <summary>Fetch the specified image for the specified release, in the specified size.</summary>
  /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
  /// <param name="id">
  /// The ID of the requested image (as found via <see cref="Image.Id"/>, or "front"/"back" as special case).
  /// </param>
  /// <param name="size">
  /// The requested image size; <see cref="CoverArtImageSize.Original"/> can be any content type, but the thumbnails are always
  /// JPEG.
  /// </param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>An asynchronous operation returning the requested image data.</returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP error status; the most common cases will be:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release and/or the specified image do not exist;
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  /// <exception cref="HttpRequestException">
  /// When the request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or
  /// timeout.
  /// </exception>
  /// <exception cref="TaskCanceledException">
  /// When the request failed due to timeout (.NET Core and .NET 5 and later only).
  /// </exception>
  public Task<CoverArtImage> FetchImageAsync(Guid mbid, string id, CoverArtImageSize size = CoverArtImageSize.Original,
                                             CancellationToken cancellationToken = new())
    => this.FetchImageAsync("release", mbid, id, size, cancellationToken);

  /// <summary>Fetch information about the cover art associated with the specified MusicBrainz release (if any).</summary>
  /// <param name="mbid">The MusicBrainz release ID for which cover art information is requested.</param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>
  /// An asynchronous operation returning an <see cref="IRelease"/> object containing information about the cover art for the
  /// release.
  /// </returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP error status; the most common cases will be:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no associated cover art);
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  /// <exception cref="HttpRequestException">
  /// When the request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or
  /// timeout.
  /// </exception>
  /// <exception cref="TaskCanceledException">
  /// When the request failed due to timeout (.NET Core and .NET 5 and later only).
  /// </exception>
  public Task<IRelease> FetchReleaseAsync(Guid mbid, CancellationToken cancellationToken = new())
    => this.FetchReleaseAsync("release", mbid, cancellationToken);

  /// <summary>Fetch information about the cover art associated with the specified MusicBrainz release (if any).</summary>
  /// <param name="mbid">The MusicBrainz release ID for which cover art information is requested.</param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>
  /// An asynchronous operation returning an <see cref="IRelease"/> object containing information about the cover art for the
  /// release, or <see langword="null"/> if the release does not exist (or has no associated cover art).
  /// </returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP error status; the most common case will be status 503
  /// (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </exception>
  /// <exception cref="HttpRequestException">
  /// When the request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or
  /// timeout.
  /// </exception>
  /// <exception cref="TaskCanceledException">
  /// When the request failed due to timeout (.NET Core and .NET 5 and later only).
  /// </exception>
  public Task<IRelease?> FetchReleaseIfAvailableAsync(Guid mbid, CancellationToken cancellationToken = new())
    => this.FetchReleaseIfAvailableAsync("release", mbid, cancellationToken);

}
