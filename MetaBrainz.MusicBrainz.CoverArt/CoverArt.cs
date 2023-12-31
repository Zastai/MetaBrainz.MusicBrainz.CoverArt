using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
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
public partial class CoverArt : IDisposable {

  #region Constants

  /// <summary>
  /// The maximum allowed image size; an exception is thrown if a response larger than this is received from the CoverArt Archive.
  /// </summary>
  /// <remarks>
  /// The CoverArt does not actually impose a file size limit; however, a limit of 512MiB seems fairly sensible (and can easily be
  /// raised if larger cover art is ever encountered).
  /// </remarks>
  public const int MaxImageSize = 512 * 1024 * 1024;

  /// <summary>The URL included in the user agent for requests as part of this library's information.</summary>
  public const string UserAgentUrl = "https://github.com/Zastai/MetaBrainz.MusicBrainz.CoverArt";

  #endregion

  #region Static Fields / Properties

  private static int _defaultPort = -1;

  /// <summary>The default port number to use for requests (-1 to not specify any explicit port).</summary>
  public static int DefaultPort {
    get => CoverArt._defaultPort;
    set {
      if (value is < -1 or > 65535) {
        throw new ArgumentOutOfRangeException(nameof(CoverArt.DefaultPort), value,
                                              "The default port number must not be less than -1 or greater than 65535.");
      }
      CoverArt._defaultPort = value;
    }
  }

  private static string _defaultServer = "coverartarchive.org";

  /// <summary>The default server to use for requests.</summary>
  public static string DefaultServer {
    get => CoverArt._defaultServer;
    set {
      if (string.IsNullOrWhiteSpace(value)) {
        throw new ArgumentException("The default server name must not be blank.", nameof(CoverArt.DefaultServer));
      }
      CoverArt._defaultServer = value.Trim();
    }
  }

  private static string _defaultUrlScheme = "https";

  /// <summary>The default URL scheme (internet access protocol) to use for requests.</summary>
  public static string DefaultUrlScheme {
    get => CoverArt._defaultUrlScheme;
    set {
      if (string.IsNullOrWhiteSpace(value)) {
        throw new ArgumentException("The default URL scheme must not be blank.", nameof(CoverArt.DefaultUrlScheme));
      }
      CoverArt._defaultUrlScheme = value.Trim();
    }
  }

  /// <summary>The default user agent values to use for requests.</summary>
  public static IList<ProductInfoHeaderValue> DefaultUserAgent { get; } = new List<ProductInfoHeaderValue>();

  /// <summary>The trace source (named 'MetaBrainz.MusicBrainz.CoverArt') used by this class.</summary>
  public static readonly TraceSource TraceSource = new("MetaBrainz.MusicBrainz.CoverArt", SourceLevels.Off);

  #endregion

  #region Constructors

  /// <summary>
  /// Initializes a new CoverArt Archive API client instance.<br/>
  /// An HTTP client will be created when needed and can be discarded again via the <see cref="Close()"/> method.
  /// </summary>
  public CoverArt() {
    this._clientOwned = true;
  }

  /// <summary>Initializes a new CoverArt Archive API client instance using a specific HTTP client.</summary>
  /// <param name="client">The HTTP client to use.</param>
  /// <param name="takeOwnership">
  /// Indicates whether this CoverArt Archive API client should take ownership of <paramref name="client"/>.<br/>
  /// If this is <see langword="false"/>, it remains owned by the caller; this means <see cref="Close()"/> will throw an exception
  /// and <see cref="Dispose()"/> will release the reference to <paramref name="client"/> without disposing it.<br/>
  /// If this is <see langword="true"/>, then this object takes ownership and treat it just like an HTTP client it created itself;
  /// this means <see cref="Close()"/> will dispose of it (with further requests creating a new HTTP client) and
  /// <see cref="Dispose()"/> will dispose the HTTP client too. Note that in this case, any default request headers set on
  /// <paramref name="client"/> will <em>not</em> be saved and used for further clients.
  /// </param>
  public CoverArt(HttpClient client, bool takeOwnership = false) {
    this._client = client;
    this._clientOwned = takeOwnership;
  }

  /// <summary>
  /// Initializes a new CoverArt Archive API client instance.<br/>
  /// An HTTP client will be created when needed and can be discarded again via the <see cref="Close()"/> method.
  /// </summary>
  /// <param name="userAgent">The user agent values to use for all requests.</param>
  public CoverArt(params ProductInfoHeaderValue[] userAgent) : this() {
    this._userAgent.AddRange(userAgent);
  }

  /// <summary>
  /// Initializes a new CoverArt Archive API client instance.<br/>
  /// An HTTP client will be created when needed and can be discarded again via the <see cref="Close()"/> method.
  /// </summary>
  /// <param name="application">The application name to use in the user agent property for all requests.</param>
  /// <param name="version">The version number to use in the user agent property for all requests.</param>
  public CoverArt(string application, Version? version) : this(application, version?.ToString()) {
  }

  /// <summary>
  /// Initializes a new CoverArt Archive API client instance.<br/>
  /// An HTTP client will be created when needed and can be discarded again via the <see cref="Close()"/> method.
  /// </summary>
  /// <param name="application">The application name to use in the user agent property for all requests.</param>
  /// <param name="version">The version number to use in the user agent property for all requests.</param>
  /// <param name="contact">
  /// The contact address (typically HTTP[S] or MAILTO) to use in the user agent property for all requests.
  /// </param>
  public CoverArt(string application, Version? version, Uri contact) : this(application, version?.ToString(), contact.ToString()) {
  }

  /// <summary>
  /// Initializes a new CoverArt Archive API client instance.<br/>
  /// An HTTP client will be created when needed and can be discarded again via the <see cref="Close()"/> method.
  /// </summary>
  /// <param name="application">The application name to use in the user agent property for all requests.</param>
  /// <param name="version">The version number to use in the user agent property for all requests.</param>
  /// <param name="contact">
  /// The contact address (typically a URL or email address) to use in the user agent property for all requests.
  /// </param>
  public CoverArt(string application, Version? version, string contact) : this(application, version?.ToString(), contact) { }

  /// <summary>
  /// Initializes a new CoverArt Archive API client instance.<br/>
  /// An HTTP client will be created when needed and can be discarded again via the <see cref="Close()"/> method.
  /// </summary>
  /// <param name="application">The application name to use in the user agent property for all requests.</param>
  /// <param name="version">The version number to use in the user agent property for all requests.</param>
  public CoverArt(string application, string? version) : this() {
    this._userAgent.Add(new ProductInfoHeaderValue(application, version));
  }

  /// <summary>
  /// Initializes a new CoverArt Archive API client instance.<br/>
  /// An HTTP client will be created when needed and can be discarded again via the <see cref="Close()"/> method.
  /// </summary>
  /// <param name="application">The application name to use in the user agent property for all requests.</param>
  /// <param name="version">The version number to use in the user agent property for all requests.</param>
  /// <param name="contact">
  /// The contact address (typically HTTP[S] or MAILTO) to use in the user agent property for all requests.
  /// </param>
  public CoverArt(string application, string? version, Uri contact) : this(application, version, contact.ToString()) { }

  /// <summary>
  /// Initializes a new CoverArt Archive API client instance.<br/>
  /// An HTTP client will be created when needed and can be discarded again via the <see cref="Close()"/> method.
  /// </summary>
  /// <param name="application">The application name to use in the user agent property for all requests.</param>
  /// <param name="version">The version number to use in the user agent property for all requests.</param>
  /// <param name="contact">
  /// The contact address (typically a URL or email address) to use in the user agent property for all requests.
  /// </param>
  public CoverArt(string application, string? version, string contact) : this() {
    this._userAgent.Add(new ProductInfoHeaderValue(application, version));
    this._userAgent.Add(new ProductInfoHeaderValue($"({contact})"));
  }

  #endregion

  #region Instance Fields / Properties

  /// <summary>The base URI for all requests.</summary>
  public Uri BaseUri => new UriBuilder(this.UrlScheme, this.Server, this.Port).Uri;

  private int _port = CoverArt.DefaultPort;

  /// <summary>The port number to use for requests (-1 to not specify any explicit port).</summary>
  public int Port {
    get => this._port;
    set {
      if (value is < -1 or > 65535) {
        throw new ArgumentOutOfRangeException(nameof(CoverArt.Port), value,
                                              "The port number must not be less than -1 or greater than 65535.");
      }
      this._port = value;
    }
  }

  private string _server = CoverArt.DefaultServer;

  /// <summary>The server to use for requests.</summary>
  public string Server {
    get => this._server;
    set {
      if (string.IsNullOrWhiteSpace(value)) {
        throw new ArgumentException("The server name must not be blank.", nameof(CoverArt.Server));
      }
      this._server = value.Trim();
    }
  }

  private string _urlScheme = CoverArt.DefaultUrlScheme;

  /// <summary>The URL scheme (internet access protocol) to use for requests.</summary>
  public string UrlScheme {
    get => this._urlScheme;
    set {
      if (string.IsNullOrWhiteSpace(value)) {
        throw new ArgumentException("The URL scheme must not be blank.", nameof(CoverArt.UrlScheme));
      }
      this._urlScheme = value.Trim();
    }
  }

  /// <summary>The user agent values to use for requests.</summary>
  /// <remarks>
  /// Note that changes to this list only take effect when a new HTTP client is created. The <see cref="Close()"/> method can be
  /// used to close the current client (if there is one) so that the next request creates a new client.
  /// </remarks>
  public IList<ProductInfoHeaderValue> UserAgent => this._userAgent;

  #endregion

  #region Public API

  /// <summary>Fetch the main "back" image for the specified release.</summary>
  /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
  /// <param name="size">
  /// The requested image size; <see cref="CoverArtImageSize.Original"/> can be any content type, but the thumbnails are always
  /// JPEG.
  /// </param>
  /// <returns>The requested image data.</returns>
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
  /// <returns>The requested image data.</returns>
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
  /// <returns>The requested image data.</returns>
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
  /// <returns>
  /// An <see cref="IRelease"/> object containing information about the cover art for the release group's main release.
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
  public IRelease FetchGroupRelease(Guid mbid) => AsyncUtils.ResultOf(this.FetchGroupReleaseAsync(mbid));

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
  /// <returns>
  /// An <see cref="IRelease"/> object containing information about the cover art for the release group's main release, or
  /// <see langword="null"/> if the release group does not exist (or has no associated cover art).
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
  public IRelease? FetchGroupReleaseIfAvailable(Guid mbid) => AsyncUtils.ResultOf(this.FetchGroupReleaseIfAvailableAsync(mbid));

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
  /// <returns>The requested image data.</returns>
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
  /// <returns>An <see cref="IRelease"/> object containing information about the cover art for the release.</returns>
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
  public IRelease FetchRelease(Guid mbid) => AsyncUtils.ResultOf(this.FetchReleaseAsync(mbid));

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
  /// <returns>
  /// An <see cref="IRelease"/> object containing information about the cover art for the release, or <see langword="null"/> if
  /// the release does not exist (or has no associated cover art).
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
  public IRelease? FetchReleaseIfAvailable(Guid mbid) => AsyncUtils.ResultOf(this.FetchReleaseIfAvailableAsync(mbid));

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

  #endregion

  #region HTTP Client / IDisposable

  private static readonly MediaTypeWithQualityHeaderValue AcceptHeader = new("application/json");

  private static readonly ProductInfoHeaderValue LibraryComment = new($"({CoverArt.UserAgentUrl})");

  private static readonly ProductInfoHeaderValue LibraryProductInfo = HttpUtils.CreateUserAgentHeader<CoverArt>();

  private HttpClient? _client;

  private Action<HttpClient>? _clientConfiguration;

  private Func<HttpClient>? _clientCreation;

  private readonly bool _clientOwned;

  private bool _disposed;

  private readonly List<ProductInfoHeaderValue> _userAgent = new(CoverArt.DefaultUserAgent);

  private HttpClient Client {
    get {
#if NET6_0
      if (this._disposed) {
        throw new ObjectDisposedException(nameof(CoverArt));
      }
#else
      ObjectDisposedException.ThrowIf(this._disposed, typeof(CoverArt));
#endif
      if (this._client is null) {
        var client = this._clientCreation?.Invoke() ?? new HttpClient();
        this._userAgent.ForEach(client.DefaultRequestHeaders.UserAgent.Add);
        this._clientConfiguration?.Invoke(client);
        this._client = client;
      }
      return this._client;
    }
  }

  /// <summary>Closes the underlying web service client in use by this CoverArt Archive client, if there is one.</summary>
  /// <remarks>The next web service request will create a new client.</remarks>
  /// <exception cref="InvalidOperationException">When this instance is using an explicitly provided client instance.</exception>
  public void Close() {
    if (!this._clientOwned) {
      throw new InvalidOperationException("An explicitly provided client instance is in use.");
    }
    Interlocked.Exchange(ref this._client, null)?.Dispose();
  }

  /// <summary>Sets up code to run to configure a newly-created HTTP client.</summary>
  /// <param name="code">The configuration code for an HTTP client, or <see langword="null"/> to clear such code.</param>
  /// <remarks>The configuration code will be called <em>after</em> <see cref="UserAgent"/> is applied.</remarks>
  public void ConfigureClient(Action<HttpClient>? code) {
    this._clientConfiguration = code;
  }

  /// <summary>Sets up code to run to create an HTTP client.</summary>
  /// <param name="code">The creation code for an HTTP client, or <see langword="null"/> to clear such code.</param>
  /// <remarks>
  /// <see cref="UserAgent"/> and any code set via <see cref="ConfigureClient(System.Action{System.Net.Http.HttpClient}?)"/> will be
  /// applied to the client returned by <paramref name="code"/>.
  /// </remarks>
  public void ConfigureClientCreation(Func<HttpClient>? code) {
    this._clientCreation = code;
  }

  /// <summary>Discards any and all resources held by this CoverArt Archive client.</summary>
  /// <remarks>Further attempts at web service requests will cause <see cref="ObjectDisposedException"/> to be thrown.</remarks>
  public void Dispose() {
    this.Dispose(true);
    GC.SuppressFinalize(this);
  }

  private void Dispose(bool disposing) {
    if (!disposing) {
      // no unmanaged resources
      return;
    }
    try {
      if (this._clientOwned) {
        this.Close();
      }
      this._client = null;
    }
    finally {
      this._disposed = true;
    }
  }

  /// <summary>Finalizes this instance, releasing any and all resources.</summary>
  ~CoverArt() {
    this.Dispose(false);
  }

  #endregion

  #region Internals

  private static readonly JsonSerializerOptions JsonReaderOptions = JsonUtils.CreateReaderOptions(Converters.Readers);

  #region Basic Request Execution

  private async Task<CoverArtImage> FetchImageAsync(string entity, Guid mbid, string id, CoverArtImageSize size,
                                                    CancellationToken cancellationToken) {
    var suffix = string.Empty;
    if (size != CoverArtImageSize.Original) {
      suffix = "-" + ((int) size).ToString(CultureInfo.InvariantCulture);
    }
    var endPoint = $"{entity}/{mbid:D}/{id}{suffix}";
    using var response = await this.PerformRequestAsync(HttpMethod.Get, endPoint, cancellationToken).ConfigureAwait(false);
    var contentLength = response.Content.Headers.ContentLength ?? 0;
    if (contentLength > CoverArt.MaxImageSize) {
      throw new ArgumentException($"The requested image is too large ({contentLength} > {CoverArt.MaxImageSize}).");
    }
    var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    await using var _ = stream.ConfigureAwait(false);
    if (stream is null) {
      throw new WebException("No data received.", WebExceptionStatus.ReceiveFailure);
    }
    var data = new MemoryStream();
    try {
      await stream.CopyToAsync(data, 64 * 1024, cancellationToken).ConfigureAwait(false);
    }
    catch {
      await data.DisposeAsync().ConfigureAwait(false);
      throw;
    }
    return new CoverArtImage(id, size, response.Content.Headers.ContentType?.MediaType, data);
  }

  private async Task<IRelease> FetchReleaseAsync(string entity, Guid mbid, CancellationToken cancellationToken) {
    var endPoint = $"{entity}/{mbid:D}";
    using var response = await this.PerformRequestAsync(HttpMethod.Get, endPoint, cancellationToken).ConfigureAwait(false);
    return await CoverArt.ParseReleaseAsync(response, cancellationToken);
  }

  private async Task<IRelease?> FetchReleaseIfAvailableAsync(string entity, Guid mbid, CancellationToken cancellationToken) {
    var endPoint = $"{entity}/{mbid:D}";
    using var response = await this.PerformRequestAsync(HttpMethod.Get, endPoint, cancellationToken).ConfigureAwait(false);
    if (response.StatusCode == HttpStatusCode.NotFound) {
      return null;
    }
    return await CoverArt.ParseReleaseAsync(response, cancellationToken);
  }

  private static async Task<IRelease> ParseReleaseAsync(HttpResponseMessage response, CancellationToken cancellationToken) {
    var jsonTask = JsonUtils.GetJsonContentAsync<Release>(response, CoverArt.JsonReaderOptions, cancellationToken);
    return await jsonTask.ConfigureAwait(false) ?? throw new JsonException("Received a null release.");
  }

  // Error Response Contents:
  //   <!doctype html>
  //   <html lang=en>
  //   <title>404 Not Found</title>
  //   <h1>Not Found</h1>
  //   <p>No cover art found for release 968db8b7-c519-43e5-bb45-9f244c92b670</p>
#if NET7_0_OR_GREATER
  [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)]
#endif
  private const string ErrorResponseContentPatternText =
    @"^(?:.*\n)*\s*<title>(\d+)?\s*(.*?)\s*</title>\s*<h1>\s*(.*?)\s*</h1>\s*<p>\s*(.*?)\s*</p>\s*$";

#if NET6_0
  private static readonly Regex TheErrorResponseContentPattern = new(CoverArt.ErrorResponseContentPatternText);

  private static Regex ErrorResponseContentPattern() => CoverArt.TheErrorResponseContentPattern;

#else

  [GeneratedRegex(CoverArt.ErrorResponseContentPatternText)]
  private static partial Regex ErrorResponseContentPattern();

#endif

  private async Task<HttpResponseMessage> PerformRequestAsync(HttpMethod method, string endPoint,
                                                              CancellationToken cancellationToken) {
    using var request = new HttpRequestMessage(method, new UriBuilder(this.UrlScheme, this.Server, this.Port, endPoint).Uri);
    var ts = CoverArt.TraceSource;
    ts.TraceEvent(TraceEventType.Verbose, 1, "WEB SERVICE REQUEST: {0} {1}", method.Method, request.RequestUri);
    var client = this.Client;
    {
      var headers = request.Headers;
      headers.Accept.Add(CoverArt.AcceptHeader);
      // Use whatever user agent the client has set, plus our own.
      {
        var userAgent = headers.UserAgent;
        foreach (var ua in client.DefaultRequestHeaders.UserAgent) {
          userAgent.Add(ua);
        }
        userAgent.Add(CoverArt.LibraryProductInfo);
        userAgent.Add(CoverArt.LibraryComment);
      }
    }
    if (ts.Switch.ShouldTrace(TraceEventType.Verbose)) {
      ts.TraceEvent(TraceEventType.Verbose, 2, "HEADERS: {0}", TextUtils.FormatMultiLine(request.Headers.ToString()));
      // There is never a body, so nothing else to trace
    }
    var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    if (ts.Switch.ShouldTrace(TraceEventType.Verbose)) {
      ts.TraceEvent(TraceEventType.Verbose, 3, "RESPONSE: {0:D}/{0} '{1}' (v{2})", response.StatusCode, response.ReasonPhrase,
                    response.Version);
      ts.TraceEvent(TraceEventType.Verbose, 4, "HEADERS: {0}", TextUtils.FormatMultiLine(response.Headers.ToString()));
      var headers = response.Content.Headers;
      ts.TraceEvent(TraceEventType.Verbose, 5, "CONTENT ({0}): {1} byte(s)", headers.ContentType, headers.ContentLength ?? 0);
    }
    try {
      return await response.EnsureSuccessfulAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (HttpError error) {
      if (!string.IsNullOrEmpty(error.Content) && error.ContentHeaders?.ContentType?.MediaType == "text/html") {
        var match = CoverArt.ErrorResponseContentPattern().Match(error.Content);
        if (match.Success) {
          var code = match.Groups[1].Success ? match.Groups[1].Value : null;
          var title = match.Groups[2].Value;
          var heading = match.Groups[3].Value;
          var message = match.Groups[4].Value;
          if (int.TryParse(code, NumberStyles.None, CultureInfo.InvariantCulture, out var status)) {
            if (status != (int) error.Status) {
              ts.TraceEvent(TraceEventType.Verbose, 5, "STATUS CODE MISMATCH: {0} <> {1}", status, (int) error.Status);
            }
          }
          else {
            ts.TraceEvent(TraceEventType.Verbose, 6, "STATUS CODE MISSING FROM TITLE");
            status = (int) error.Status;
          }
          if (title != heading) {
            ts.TraceEvent(TraceEventType.Verbose, 7, "TITLE/HEADING MISMATCH: '{0}' <> '{1}'", title, heading);
            message = $"{heading}: {message}";
          }
          throw new HttpError((HttpStatusCode) status, title, error.Version, message, error);
        }
      }
      throw;
    }
  }

  #endregion

  #endregion

}
