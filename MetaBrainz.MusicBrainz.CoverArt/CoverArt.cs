using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

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
    this.UserAgentContact = new ProductInfoHeaderValue($"({contact})");
    this.UserAgentProduct = new ProductInfoHeaderValue(product);
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
  /// <exception cref="WebException">
  /// When something went wrong with the request.
  /// More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
  /// Possible status codes for the response are:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no "back" image set);
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  public CoverArtImage FetchBack(Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original)
    => CoverArt.ResultOf(this.FetchImageAsync("release", mbid, "back", size));

  /// <summary>Fetch the main "back" image for the specified release.</summary>
  /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
  /// <param name="size">
  /// The requested image size; <see cref="CoverArtImageSize.Original"/> can be any content type, but the thumbnails are always
  /// JPEG.
  /// </param>
  /// <returns>An asynchronous operation returning the requested image data.</returns>
  /// <exception cref="WebException">
  /// When something went wrong with the request.
  /// More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
  /// Possible status codes for the response are:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no "back" image set);
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  public Task<CoverArtImage> FetchBackAsync(Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original)
    => this.FetchImageAsync("release", mbid, "back", size);

  /// <summary>Fetch the main "front" image for the specified release, in the specified size.</summary>
  /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
  /// <param name="size">
  /// The requested image size; <see cref="CoverArtImageSize.Original"/> can be any content type, but the thumbnails are always
  /// JPEG.
  /// </param>
  /// <returns>The requested image data.</returns>
  /// <exception cref="WebException">
  /// When something went wrong with the request.
  /// More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
  /// Possible status codes for the response are:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no "fromt" image set);
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  public CoverArtImage FetchFront(Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original)
    => CoverArt.ResultOf(this.FetchImageAsync("release", mbid, "front", size));

  /// <summary>Fetch the main "front" image for the specified release, in the specified size.</summary>
  /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
  /// <param name="size">
  /// The requested image size; <see cref="CoverArtImageSize.Original"/> can be any content type, but the thumbnails are always
  /// JPEG.
  /// </param>
  /// <returns>An asynchronous operation returning the requested image data.</returns>
  /// <exception cref="WebException">
  /// When something went wrong with the request.
  /// More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
  /// Possible status codes for the response are:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no "front" image set);
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  public Task<CoverArtImage> FetchFrontAsync(Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original)
    => this.FetchImageAsync("release", mbid, "front", size);

  /// <summary>Fetch the main "front" image for the specified release group, in the specified size.</summary>
  /// <param name="mbid">The MusicBrainz release group ID for which the image is requested.</param>
  /// <param name="size">
  /// The requested image size; <see cref="CoverArtImageSize.Original"/> can be any content type, but the thumbnails are always
  /// JPEG.
  /// </param>
  /// <returns>The requested image data.</returns>
  /// <exception cref="WebException">
  /// When something went wrong with the request.
  /// More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
  /// Possible status codes for the response are:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release group does not exist (or has no "front" image set);
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  public CoverArtImage FetchGroupFront(Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original)
    => CoverArt.ResultOf(this.FetchImageAsync("release-group", mbid, "front", size));

  /// <summary>Fetch the main "front" image for the specified release group, in the specified size.</summary>
  /// <param name="mbid">The MusicBrainz release group ID for which the image is requested.</param>
  /// <param name="size">
  /// The requested image size; <see cref="CoverArtImageSize.Original"/> can be any content type, but the thumbnails are always
  /// JPEG.
  /// </param>
  /// <returns>An asynchronous operation returning the requested image data.</returns>
  /// <exception cref="WebException">
  /// When something went wrong with the request.
  /// More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
  /// Possible status codes for the response are:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release group does not exist (or has no "front" image set);
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  public Task<CoverArtImage> FetchGroupFrontAsync(Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original)
    => this.FetchImageAsync("release-group", mbid, "front", size);

  /// <summary>Fetch information about the cover art associated with the specified MusicBrainz release group (if any).</summary>
  /// <param name="mbid">The MusicBrainz release group ID for which cover art information is requested.</param>
  /// <returns>
  /// A <see cref="Release"/> object containing information about the cover art for the release group's main release.
  /// </returns>
  /// <exception cref="WebException">
  /// When something went wrong with the request.
  /// More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
  /// Possible status codes for the response are:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release group does not exist (or has no associated cover art);
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  public IRelease FetchGroupRelease(Guid mbid) => CoverArt.ResultOf(this.FetchReleaseAsync("release-group", mbid));

  /// <summary>Fetch information about the cover art associated with the specified MusicBrainz release group (if any).</summary>
  /// <param name="mbid">The MusicBrainz release group ID for which cover art information is requested.</param>
  /// <returns>
  /// An asynchronous operation returning a <see cref="Release"/> object containing information about the cover art for the
  /// release group's main release.
  /// </returns>
  /// <exception cref="WebException">
  /// When something went wrong with the request.
  /// More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
  /// Possible status codes for the response are:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release group does not exist (or has no associated cover art);
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  public Task<IRelease> FetchGroupReleaseAsync(Guid mbid) => this.FetchReleaseAsync("release-group", mbid);

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
  /// <exception cref="WebException">
  /// When something went wrong with the request.
  /// More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
  /// Possible status codes for the response are:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release and/or the specified image do not exist;
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  public CoverArtImage FetchImage(Guid mbid, string id, CoverArtImageSize size = CoverArtImageSize.Original)
    => CoverArt.ResultOf(this.FetchImageAsync("release", mbid, id, size));

  /// <summary>Fetch the specified image for the specified release, in the specified size.</summary>
  /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
  /// <param name="id">
  /// The ID of the requested image (as found via <see cref="Image.Id"/>, or "front"/"back" as special case).
  /// </param>
  /// <param name="size">
  /// The requested image size; <see cref="CoverArtImageSize.Original"/> can be any content type, but the thumbnails are always
  /// JPEG.
  /// </param>
  /// <returns>An asynchronous operation returning the requested image data.</returns>
  /// <exception cref="WebException">
  /// When something went wrong with the request.
  /// More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
  /// Possible status codes for the response are:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release and/or the specified image do not exist;
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  public Task<CoverArtImage> FetchImageAsync(Guid mbid, string id, CoverArtImageSize size = CoverArtImageSize.Original)
    => this.FetchImageAsync("release", mbid, id, size);

  /// <summary>Fetch information about the cover art associated with the specified MusicBrainz release (if any).</summary>
  /// <param name="mbid">The MusicBrainz release ID for which cover art information is requested.</param>
  /// <returns>A <see cref="Release"/> object containing information about the cover art for the release.</returns>
  /// <exception cref="WebException">
  /// When something went wrong with the request.
  /// More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
  /// Possible status codes for the response are:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no associated cover art);
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  public IRelease FetchRelease(Guid mbid) => CoverArt.ResultOf(this.FetchReleaseAsync("release", mbid));

  /// <summary>Fetch information about the cover art associated with the specified MusicBrainz release (if any).</summary>
  /// <param name="mbid">The MusicBrainz release ID for which cover art information is requested.</param>
  /// <returns>
  /// An asynchronous operation returning a <see cref="Release"/> object containing information about the cover art for the
  /// release.
  /// </returns>
  /// <exception cref="WebException">
  /// When something went wrong with the request.
  /// More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
  /// Possible status codes for the response are:
  /// <ul><li>
  ///   404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no associated cover art);
  /// </li><li>
  ///   503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.
  /// </li></ul>
  /// </exception>
  public Task<IRelease> FetchReleaseAsync(Guid mbid) => this.FetchReleaseAsync("release", mbid);

  #endregion

  #region Internals

  #region JSON Options

  private static readonly JsonSerializerOptions JsonReaderOptions = JsonUtils.CreateReaderOptions(Converters.Readers);

  #endregion

  #region HTTP Client / IDisposable

  private bool Disposed;

  private HttpClient? TheClient;

  private readonly ProductInfoHeaderValue UserAgentContact;

  private readonly ProductInfoHeaderValue UserAgentProduct;

  private HttpClient Client {
    get {
      if (this.Disposed) {
        throw new ObjectDisposedException(nameof(CoverArt));
      }
      if (this.TheClient == null) { // Set up the instance with the invariant settings
        var an = typeof(CoverArt).Assembly.GetName();
        this.TheClient = new HttpClient {
          BaseAddress = new UriBuilder("https", this.Server, this.Port).Uri,
          DefaultRequestHeaders = {
              Accept = {
                new MediaTypeWithQualityHeaderValue("application/json")
              },
              UserAgent = {
                this.UserAgentProduct,
                this.UserAgentContact,
                new ProductInfoHeaderValue(an.Name ?? "*Unknown Assembly*", an.Version?.ToString()),
                new ProductInfoHeaderValue($"({CoverArt.UserAgentUrl})"),
              },
            }
        };
      }
      return this.TheClient;
    }
  }

  /// <summary>Closes the underlying web service client in use by this CoverArt Archive client, if there is one.</summary>
  /// <remarks>The next web service request will create a new client.</remarks>
  public void Close() {
    Interlocked.Exchange(ref this.TheClient, null)?.Dispose();
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
      this.Disposed = true;
    }
  }

  /// <summary>Finalizes this instance.</summary>
  ~CoverArt() {
    this.Dispose(false);
  }

  #endregion

  #region Basic Request Execution

  private async Task<HttpResponseMessage> PerformRequestAsync(string address) {
    Debug.Print($"[{DateTime.UtcNow}] CAA REQUEST: GET {this.BaseUri}{address}");
    var client = this.Client;
    var request = new HttpRequestMessage(HttpMethod.Get, address);
    var response = await client.SendAsync(request);
    Debug.Print($"[{DateTime.UtcNow}] => RESPONSE: {(int) response.StatusCode}/{response.StatusCode} '{response.ReasonPhrase}' (v{response.Version})");
    Debug.Print($"[{DateTime.UtcNow}] => HEADERS: {CoverArt.FormatMultiLine(response.Headers.ToString())}");
    Debug.Print($"[{DateTime.UtcNow}] => CONTENT: {response.Content.Headers.ContentType}, {response.Content.Headers.ContentLength ?? 0} byte(s))");
    return response;
  }

  private async Task<CoverArtImage> FetchImageAsync(string entity, Guid mbid, string id, CoverArtImageSize size) {
    var suffix = string.Empty;
    if (size != CoverArtImageSize.Original) {
      suffix = "-" + ((int) size).ToString(CultureInfo.InvariantCulture);
    }
    var address= $"{entity}/{mbid:D}/{id}{suffix}";
    using var response = await this.PerformRequestAsync(address).ConfigureAwait(false);
    var contentLength = response.Content.Headers.ContentLength ?? 0;
    if (contentLength > CoverArt.MaxImageSize) {
      throw new ArgumentException($"The requested image is too large ({contentLength} > {CoverArt.MaxImageSize}).");
    }
#if NET || NETSTANDARD2_1_OR_GREATER
    var stream = await response.Content.ReadAsStreamAsync();
    await using var _ = stream.ConfigureAwait(false);
#else
    using var stream = await response.Content.ReadAsStreamAsync();
#endif
    if (stream == null) {
      throw new WebException("No data received.", WebExceptionStatus.ReceiveFailure);
    }
    var data = new MemoryStream();
    try {
      await stream.CopyToAsync(data).ConfigureAwait(false);
    }
    catch {
      data.Dispose();
      throw;
    }
    return new CoverArtImage(id, size, response.Content?.Headers?.ContentType?.MediaType, data);
  }

  private async Task<IRelease> FetchReleaseAsync(string entity, Guid mbid) {
    using var response = await this.PerformRequestAsync($"{entity}/{mbid:D}").ConfigureAwait(false);
#if NET || NETSTANDARD2_1_OR_GREATER
    var stream = await response.Content.ReadAsStreamAsync();
    await using var _ = stream.ConfigureAwait(false);
#else
    using var stream = await response.Content.ReadAsStreamAsync();
#endif
    if (stream == null) {
      throw new WebException("No data received.", WebExceptionStatus.ReceiveFailure);
    }
    var characterSet = response.Content?.Headers?.ContentType?.CharSet;
    if (string.IsNullOrWhiteSpace(characterSet)) {
      characterSet = "utf-8";
    }
    IRelease? release;
#if !DEBUG
    if (characterSet == "utf-8") { // Directly use the stream
      release = await JsonUtils.DeserializeAsync<Release>(stream, CoverArt.JsonReaderOptions);
      return release ?? throw new JsonException("Received a null release.");
    }
#endif
    var enc = Encoding.GetEncoding(characterSet);
    using var sr = new StreamReader(stream, enc, false, 1024, true);
    var json = await sr.ReadToEndAsync().ConfigureAwait(false);
    Debug.Print($"[{DateTime.UtcNow}] => JSON: {JsonUtils.Prettify(json)}");
    release = JsonUtils.Deserialize<Release>(json, CoverArt.JsonReaderOptions);
    return release ?? throw new JsonException("Received a null release.");
  }

  #endregion

  #region Utility Methods

  private static string FormatMultiLine(string text) {
    const string prefix = "<<";
    const string suffix = ">>";
    const string sep = "\n  ";
    char[] newlines = { '\r', '\n' };
    text = text.Replace("\r\n", "\n").TrimEnd(newlines);
    var lines = text.Split(newlines);
    if (lines.Length == 0) {
      return prefix + suffix;
    }
    if (lines.Length == 1) {
      return prefix + lines[0] + suffix;
    }
    return prefix + sep + string.Join(sep, lines) + "\n" + suffix;
  }

  private static Uri GetDefaultContactInfo() {
    return CoverArt.DefaultContactInfo ??
      throw new InvalidOperationException($"When not passed to a constructor, contact info needs to be set using {nameof(CoverArt.DefaultContactInfo)}.");
  }

  private static ProductHeaderValue GetDefaultProductInfo() {
    return CoverArt.DefaultProductInfo ??
      throw new InvalidOperationException($"When not passed to a constructor, product info needs to be set using {nameof(CoverArt.DefaultContactInfo)}.");
  }

  private static void ResultOf(Task task) => task.ConfigureAwait(false).GetAwaiter().GetResult();

  private static T ResultOf<T>(Task<T> task) => task.ConfigureAwait(false).GetAwaiter().GetResult();

  #endregion

  #endregion

}
