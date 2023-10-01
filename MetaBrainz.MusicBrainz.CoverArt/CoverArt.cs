using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using MetaBrainz.Common;
using MetaBrainz.Common.Json;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;
using MetaBrainz.MusicBrainz.CoverArt.Json;
using MetaBrainz.MusicBrainz.CoverArt.Objects;

namespace MetaBrainz.MusicBrainz.CoverArt;

/// <summary>Class providing access to the CoverArt Archive API.</summary>
[PublicAPI]
public class CoverArt : IDisposable {

  #region Static Fields / Properties

  /// <summary>
  /// The default contact info portion of the user agent to use for requests; used as initial value for <see cref="ContactInfo"/>.
  /// </summary>
  public static Uri? DefaultContactInfo { get; set; }

  /// <summary>The default port number to use for requests (-1 to not specify any explicit port).</summary>
  public static int DefaultPort { get; set; } = -1;

  /// <summary>
  /// The default product info portion of the user agent to use for requests; used as initial value for <see cref="ProductInfo"/>.
  /// </summary>
  public static ProductHeaderValue? DefaultProductInfo { get; set; }

  /// <summary>The default web site to use for requests.</summary>
  public static string DefaultServer{ get; set; } = "coverartarchive.org";

  /// <summary>The default internet access protocol to use for requests.</summary>
  public static string DefaultUrlScheme { get; set; } = "https";

  /// <summary>The default user agent to use for requests.</summary>
  public static string DefaultUserAgent { get; set; } = string.Empty;

  /// <summary>
  /// The maximum allowed image size; an exception is thrown if a response larger than this is received from the CoverArt Archive.
  /// </summary>
  /// <remarks>
  /// The CoverArt does not actually impose a file size limit.
  /// At the moment, the largest item in the CAA is a PDF of 236MiB, followed by a PNG of 159MiB
  /// (<a href="http://notlob.eu/caa/largeimages">source</a>).
  /// Setting the limit at 512MiB therefore seems fairly sensible.
  /// </remarks>
  public const int MaxImageSize = 512 * 1024 * 1024;

  /// <summary>The URL included in the user agent for requests as part of this library's information.</summary>
  public const string UserAgentUrl = "https://github.com/Zastai/MetaBrainz.MusicBrainz.CoverArt";

  #endregion

  #region Constructors

  /// <summary>
  /// Initializes a new CoverArt Archive API client instance.
  /// User agent information must have been set up via <see cref="DefaultContactInfo"/> and <see cref="DefaultProductInfo"/>.
  /// </summary>
  public CoverArt()
  : this(CoverArt.GetDefaultProductInfo(), CoverArt.GetDefaultContactInfo())
  { }

  /// <summary>
  /// Initializes a new CoverArt Archive API client instance.
  /// Contact information must have been set up via <see cref="DefaultContactInfo"/>.
  /// </summary>
  /// <param name="product">The product info portion of the user agent to use for requests.</param>
  public CoverArt(ProductHeaderValue product)
  : this(product, CoverArt.GetDefaultContactInfo())
  { }

  /// <summary>Initializes a new CoverArt Archive API client instance.</summary>
  /// <param name="product">The product info portion of the user agent to use for requests.</param>
  /// <param name="contact">
  /// The contact info portion (typically a URL or email address) of the user agent to use for requests.
  /// </param>
  public CoverArt(ProductHeaderValue product, Uri contact) {
    this.ContactInfo = contact;
    this.ProductInfo = product;
    this._contact = new ProductInfoHeaderValue($"({contact})");
    this._product = new ProductInfoHeaderValue(product);
  }

  /// <summary>Initializes a new CoverArt Archive API client instance.</summary>
  /// <param name="product">The product info portion of the user agent to use for requests.</param>
  /// <param name="contact">
  /// The contact info portion (typically a URL or email address) of the user agent to use for requests. Must be a valid URI.
  /// </param>
  public CoverArt(ProductHeaderValue product, string contact)
  : this(product, new Uri(contact))
  { }

  /// <summary>
  /// Initializes a new CoverArt Archive API client instance.
  /// Product information must have been set up via <see cref="DefaultProductInfo"/>.
  /// </summary>
  /// <param name="contact">
  /// The contact info portion (typically a URL or email address) of the user agent to use for requests.
  /// </param>
  public CoverArt(Uri contact)
  : this(CoverArt.GetDefaultProductInfo (), contact)
  { }

  /// <summary>
  /// Initializes a new CoverArt Archive API client instance.
  /// Product information must have been set up via <see cref="DefaultProductInfo"/>.
  /// </summary>
  /// <param name="contact">
  /// The contact info portion (typically a URL or email address) of the user agent to use for requests. Must be a valid URI.
  /// </param>
  public CoverArt(string contact)
  : this(new Uri(contact))
  { }

  /// <summary>
  /// Initializes a new CoverArt Archive API client instance.
  /// Contact information must have been set up via <see cref="DefaultContactInfo"/>.
  /// </summary>
  /// <param name="application">The application name for the product info portion of the user agent to use for requests.</param>
  /// <param name="version">The version number for the product info portion of the user agent to use for requests.</param>
  public CoverArt(string application, Version version)
  : this(application, version.ToString())
  { }

  /// <summary>
  /// Initializes a new CoverArt Archive API client instance.
  /// Contact information must have been set up via <see cref="DefaultContactInfo"/>.
  /// </summary>
  /// <param name="application">The application name for the product info portion of the user agent to use for requests.</param>
  /// <param name="version">The version number for the product info portion of the user agent to use for requests.</param>
  public CoverArt(string application, string version)
  : this(new ProductHeaderValue(application, version))
  { }

  /// <summary>Initializes a new CoverArt Archive API client instance.</summary>
  /// <param name="application">The application name for the product info portion of the user agent to use for requests.</param>
  /// <param name="version">The version number for the product info portion of the user agent to use for requests.</param>
  /// <param name="contact">
  /// The contact info portion (typically a URL or email address) of the user agent to use for requests.
  /// </param>
  public CoverArt(string application, Version version, Uri contact)
  : this(application, version.ToString(), contact)
  { }

  /// <summary>Initializes a new CoverArt Archive API client instance.</summary>
  /// <param name="application">The application name for the product info portion of the user agent to use for requests.</param>
  /// <param name="version">The version number for the product info portion of the user agent to use for requests.</param>
  /// <param name="contact">
  /// The contact info portion (typically a URL or email address) of the user agent to use for requests. Must be a valid URI.
  /// </param>
  public CoverArt(string application, Version version, string contact)
  : this(application, version.ToString(), new Uri(contact))
  { }

  /// <summary>Initializes a new CoverArt Archive API client instance.</summary>
  /// <param name="application">The application name for the product info portion of the user agent to use for requests.</param>
  /// <param name="version">The version number for the product info portion of the user agent to use for requests.</param>
  /// <param name="contact">
  /// The contact info portion (typically a URL or email address) of the user agent to use for requests.
  /// </param>
  public CoverArt(string application, string version, Uri contact)
  : this(new ProductHeaderValue(application, version), contact)
  { }

  /// <summary>Initializes a new CoverArt Archive API client instance.</summary>
  /// <param name="application">The application name to use in the User-Agent property for all requests.</param>
  /// <param name="version">The version number to use in the User-Agent property for all requests.</param>
  /// <param name="contact">
  /// The contact info portion (typically a URL or email address) of the user agent to use for requests. Must be a valid URI.
  /// </param>
  public CoverArt(string application, string version, string contact)
  : this(application, version, new Uri(contact))
  { }

  #endregion

  #region Instance Fields / Properties

  /// <summary>The base URI for all requests.</summary>
  public Uri BaseUri => new UriBuilder(this.UrlScheme, this.Server, this.Port).Uri;

  /// <summary>The contact information portion of the user agent to use for requests.</summary>
  public Uri ContactInfo { get; }

  /// <summary>
  /// The port number to use for requests (-1 to not specify any explicit port).<br/>
  /// Changes to this property only take effect when creating the underlying web service client. If this property is set after
  /// requests have been issued, <see cref="Close()"/> must be called for the changes to take effect.
  /// </summary>
  public int Port { get; set; } = CoverArt.DefaultPort;

  /// <summary>The product information portion of the user agent to use for requests.</summary>
  public ProductHeaderValue ProductInfo { get; }

  /// <summary>
  /// The server to use for requests.<br/>
  /// Changes to this property only take effect when creating the underlying web service client. If this property is set after
  /// requests have been issued, <see cref="Close()"/> must be called for the changes to take effect.
  /// </summary>
  public string Server { get; set; } = CoverArt.DefaultServer;

  /// <summary>
  /// The internet access protocol to use for requests.<br/>
  /// Changes to this property only take effect when creating the underlying web service client. If this property is set after
  /// requests have been issued, <see cref="Close()"/> must be called for the changes to take effect.
  /// </summary>
  public string UrlScheme { get; set; } = CoverArt.DefaultUrlScheme;

  #endregion

  #region Instance Methods

  /// <summary>Fetch the main "back" image for the specified release.</summary>
  /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
  /// <param name="size">
  /// The requested image size; <see cref="CoverArtImageSize.Original"/> can be any content type, but the thumbnails are always
  /// JPEG.
  /// </param>
  /// <returns>The requested image data.</returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; most common cases will be:
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
  public CoverArtImage FetchBack(Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original)
    => AsyncUtils.ResultOf(this.FetchBackAsync(mbid, size));

  /// <summary>Fetch the main "back" image for the specified release.</summary>
  /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
  /// <param name="size">
  /// The requested image size; <see cref="CoverArtImageSize.Original"/> can be any content type, but the thumbnails are always
  /// JPEG.
  /// </param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>An asynchronous operation returning the requested image data.</returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; most common cases will be:
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
  /// <returns>The requested image data.</returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; most common cases will be:
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
  public CoverArtImage FetchFront(Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original)
    => AsyncUtils.ResultOf(this.FetchFrontAsync(mbid, size));

  /// <summary>Fetch the main "front" image for the specified release, in the specified size.</summary>
  /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
  /// <param name="size">
  /// The requested image size; <see cref="CoverArtImageSize.Original"/> can be any content type, but the thumbnails are always
  /// JPEG.
  /// </param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>An asynchronous operation returning the requested image data.</returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; most common cases will be:
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
  /// <returns>The requested image data.</returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; most common cases will be:
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
  public CoverArtImage FetchGroupFront(Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original)
    => AsyncUtils.ResultOf(this.FetchGroupFrontAsync(mbid, size));

  /// <summary>Fetch the main "front" image for the specified release group, in the specified size.</summary>
  /// <param name="mbid">The MusicBrainz release group ID for which the image is requested.</param>
  /// <param name="size">
  /// The requested image size; <see cref="CoverArtImageSize.Original"/> can be any content type, but the thumbnails are always
  /// JPEG.
  /// </param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>An asynchronous operation returning the requested image data.</returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; most common cases will be:
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
  /// <returns>
  /// An <see cref="IRelease"/> object containing information about the cover art for the release group's main release.
  /// </returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; most common cases will be:
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
  public IRelease FetchGroupRelease(Guid mbid) => AsyncUtils.ResultOf(this.FetchGroupReleaseAsync(mbid));

  /// <summary>Fetch information about the cover art associated with the specified MusicBrainz release group (if any).</summary>
  /// <param name="mbid">The MusicBrainz release group ID for which cover art information is requested.</param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>
  /// An asynchronous operation returning an <see cref="IRelease"/> object containing information about the cover art for the
  /// release group's main release.
  /// </returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; most common cases will be:
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
  /// <returns>
  /// An <see cref="IRelease"/> object containing information about the cover art for the release group's main release, or
  /// <see langword="null"/> if the release group does not exist (or has no associated cover art).
  /// </returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; the most common case will
  /// be status 503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </exception>
  /// <exception cref="HttpRequestException">
  /// When the request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or
  /// timeout.
  /// </exception>
  /// <exception cref="TaskCanceledException">
  /// When the request failed due to timeout (.NET Core and .NET 5 and later only).
  /// </exception>
  public IRelease? FetchGroupReleaseIfAvailable(Guid mbid) => AsyncUtils.ResultOf(this.FetchGroupReleaseIfAvailableAsync(mbid));

  /// <summary>Fetch information about the cover art associated with the specified MusicBrainz release group (if any).</summary>
  /// <param name="mbid">The MusicBrainz release group ID for which cover art information is requested.</param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>
  /// An asynchronous operation returning an <see cref="IRelease"/> object containing information about the cover art for the
  /// release group's main release, or <see langword="null"/> if the release group does not exist (or has no associated cover art).
  /// </returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; the most common case will
  /// be status 503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
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
  /// <returns>The requested image data.</returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; most common cases will be:
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
  public CoverArtImage FetchImage(Guid mbid, string id, CoverArtImageSize size = CoverArtImageSize.Original)
    => AsyncUtils.ResultOf(this.FetchImageAsync(mbid, id, size));

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
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; most common cases will be:
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
  /// <returns>An <see cref="IRelease"/> object containing information about the cover art for the release.</returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; most common cases will be:
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
  public IRelease FetchRelease(Guid mbid) => AsyncUtils.ResultOf(this.FetchReleaseAsync(mbid));

  /// <summary>Fetch information about the cover art associated with the specified MusicBrainz release (if any).</summary>
  /// <param name="mbid">The MusicBrainz release ID for which cover art information is requested.</param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>
  /// An asynchronous operation returning an <see cref="IRelease"/> object containing information about the cover art for the
  /// release.
  /// </returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; most common cases will be:
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
  /// <returns>
  /// An <see cref="IRelease"/> object containing information about the cover art for the release, or <see langword="null"/> if
  /// the release does not exist (or has no associated cover art).
  /// </returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; the most common case will
  /// be status 503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </exception>
  /// <exception cref="HttpRequestException">
  /// When the request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or
  /// timeout.
  /// </exception>
  /// <exception cref="TaskCanceledException">
  /// When the request failed due to timeout (.NET Core and .NET 5 and later only).
  /// </exception>
  public IRelease? FetchReleaseIfAvailable(Guid mbid) => AsyncUtils.ResultOf(this.FetchReleaseIfAvailableAsync(mbid));

  /// <summary>Fetch information about the cover art associated with the specified MusicBrainz release (if any).</summary>
  /// <param name="mbid">The MusicBrainz release ID for which cover art information is requested.</param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>
  /// An asynchronous operation returning an <see cref="IRelease"/> object containing information about the cover art for the
  /// release, or <see langword="null"/> if the release does not exist (or has no associated cover art).
  /// </returns>
  /// <exception cref="HttpError">
  /// When the request succeeded but reported an HTTP status other than <see cref="HttpStatusCode.OK"/>; the most common case will
  /// be status 503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
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

  #endregion

  #region Internals

  #region JSON Options

  private static readonly JsonSerializerOptions JsonReaderOptions = JsonUtils.CreateReaderOptions(Converters.Readers);

  #endregion

  #region HTTP Client / IDisposable

  private bool _disposed;

  private HttpClient? _client;

  private readonly ProductInfoHeaderValue _contact;

  private readonly ProductInfoHeaderValue _product;

  private HttpClient Client {
    get {
      if (this._disposed) {
        throw new ObjectDisposedException(nameof(CoverArt));
      }
      if (this._client == null) { // Set up the instance with the invariant settings
        var an = typeof(CoverArt).Assembly.GetName();
        this._client = new HttpClient {
          BaseAddress = new UriBuilder("https", this.Server, this.Port).Uri,
          DefaultRequestHeaders = {
              Accept = {
                new MediaTypeWithQualityHeaderValue("application/json")
              },
              UserAgent = {
                this._product,
                this._contact,
                new ProductInfoHeaderValue(an.Name ?? "*Unknown Assembly*", an.Version?.ToString()),
                new ProductInfoHeaderValue($"({CoverArt.UserAgentUrl})"),
              },
            }
        };
      }
      return this._client;
    }
  }

  /// <summary>Closes the underlying web service client in use by this CoverArt Archive client, if there is one.</summary>
  /// <remarks>The next web service request will create a new client.</remarks>
  public void Close() {
    Interlocked.Exchange(ref this._client, null)?.Dispose();
  }

  /// <summary>Disposes the web service client in use by this CoverArt Archive client, if there is one.</summary>
  /// <remarks>Further attempts at web service requests will cause <see cref="ObjectDisposedException"/> to be thrown.</remarks>
  public void Dispose() {
    this.Dispose(true);
    GC.SuppressFinalize(this);
  }

  private void Dispose(bool disposing) {
    if (!disposing) {
      return;
    }
    try {
      this.Close();
    }
    finally {
      this._disposed = true;
    }
  }

  /// <summary>Finalizes this instance.</summary>
  ~CoverArt() {
    this.Dispose(false);
  }

  #endregion

  #region Basic Request Execution

  private async Task<CoverArtImage> FetchImageAsync(string entity, Guid mbid, string id, CoverArtImageSize size,
                                                    CancellationToken cancellationToken) {
    var suffix = string.Empty;
    if (size != CoverArtImageSize.Original) {
      suffix = "-" + ((int) size).ToString(CultureInfo.InvariantCulture);
    }
    var address= $"{entity}/{mbid:D}/{id}{suffix}";
    using var response = await this.PerformRequestAsync(address, cancellationToken).ConfigureAwait(false);
    CoverArt.ThrowIfUnsuccessful(response);
    var contentLength = response.Content.Headers.ContentLength ?? 0;
    if (contentLength > CoverArt.MaxImageSize) {
      throw new ArgumentException($"The requested image is too large ({contentLength} > {CoverArt.MaxImageSize}).");
    }
#if NET
    var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    await using var _ = stream.ConfigureAwait(false);
#elif NETSTANDARD2_1_OR_GREATER
    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
    await using var _ = stream.ConfigureAwait(false);
#else
    using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
    if (stream == null) {
      throw new WebException("No data received.", WebExceptionStatus.ReceiveFailure);
    }
    var data = new MemoryStream();
    try {
      await stream.CopyToAsync(data, 64 * 1024, cancellationToken).ConfigureAwait(false);
    }
    catch {
#if NET || NETSTANDARD2_1_OR_GREATER
      await data.DisposeAsync().ConfigureAwait(false);
#else
      data.Dispose();
#endif
      throw;
    }
    return new CoverArtImage(id, size, response.Content?.Headers?.ContentType?.MediaType, data);
  }

  private async Task<IRelease> FetchReleaseAsync(string entity, Guid mbid, CancellationToken cancellationToken) {
    using var response = await this.PerformRequestAsync($"{entity}/{mbid:D}", cancellationToken).ConfigureAwait(false);
    CoverArt.ThrowIfUnsuccessful(response);
    return await CoverArt.ParseReleaseAsync(response, cancellationToken);
  }

  private async Task<IRelease?> FetchReleaseIfAvailableAsync(string entity, Guid mbid, CancellationToken cancellationToken) {
    using var response = await this.PerformRequestAsync($"{entity}/{mbid:D}", cancellationToken).ConfigureAwait(false);
    if (response.StatusCode == HttpStatusCode.NotFound) {
      return null;
    }
    CoverArt.ThrowIfUnsuccessful(response);
    return await CoverArt.ParseReleaseAsync(response, cancellationToken);
  }

  private static async Task<IRelease> ParseReleaseAsync(HttpResponseMessage response, CancellationToken cancellationToken) {
    var jsonTask = JsonUtils.GetJsonContentAsync<Release>(response, CoverArt.JsonReaderOptions, cancellationToken);
    return await jsonTask.ConfigureAwait(false) ?? throw new JsonException("Received a null release.");
  }

  private async Task<HttpResponseMessage> PerformRequestAsync(string address, CancellationToken cancellationToken) {
    Debug.Print($"[{DateTime.UtcNow}] CAA REQUEST: GET {this.BaseUri}{address}");
    var client = this.Client;
    var request = new HttpRequestMessage(HttpMethod.Get, address);
    var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    Debug.Print($"[{DateTime.UtcNow}] => RESPONSE: {(int) response.StatusCode}/{response.StatusCode} '{response.ReasonPhrase}' " +
                $"(v{response.Version})");
    Debug.Print($"[{DateTime.UtcNow}] => HEADERS: {TextUtils.FormatMultiLine(response.Headers.ToString())}");
    Debug.Print($"[{DateTime.UtcNow}] => CONTENT: {response.Content.Headers.ContentType}, " +
                $"{response.Content.Headers.ContentLength ?? 0} byte(s))");
    return response;
  }

  private static void ThrowIfUnsuccessful(HttpResponseMessage response) {
    // FIXME: Or should this use IsSuccessStatusCode?
    if (response.StatusCode == HttpStatusCode.OK) {
      return;
    }
#if DEBUG
    string? errorInfo = null;
    if (response.Content.Headers.ContentLength > 0) {
      errorInfo = AsyncUtils.ResultOf(HttpUtils.GetStringContentAsync(response));
      if (string.IsNullOrWhiteSpace(errorInfo)) {
        Debug.Print($"[{DateTime.UtcNow}] => NO ERROR RESPONSE TEXT");
        errorInfo = null;
      }
      else {
        Debug.Print($"[{DateTime.UtcNow}] => ERROR RESPONSE TEXT: {TextUtils.FormatMultiLine(errorInfo)}");
      }
    }
    else {
      Debug.Print($"[{DateTime.UtcNow}] => NO ERROR RESPONSE CONTENT");
    }
    if (errorInfo is not null && response.Content.Headers.ContentType?.MediaType == "text/html") {
      // The contents seems to be of the form:
      //   <!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 3.2 Final//EN">
      //   <title>404 Not Found</title>
      //   <h1>Not Found</h1>
      //   <p>No cover art found for release 968db8b7-c519-43e5-bb45-9f244c92b670</p>
      // FIXME: It may make sense to try and extract the contents of that paragraph for use in the exception.
    }
#endif
    throw new HttpError(response);
  }

  #endregion

  #region Utility Methods

  private static Uri GetDefaultContactInfo() {
    const string msg = "When not passed to a constructor, contact info needs to be set using " +
                       $"{nameof(CoverArt.DefaultContactInfo)}.";
    return CoverArt.DefaultContactInfo ?? throw new InvalidOperationException(msg);
  }

  private static ProductHeaderValue GetDefaultProductInfo() {
    const string msg = "When not passed to a constructor, product info needs to be set using " +
                       $"{nameof(CoverArt.DefaultProductInfo)}.";
    return CoverArt.DefaultProductInfo ?? throw new InvalidOperationException(msg);
  }

  #endregion

  #endregion

}
