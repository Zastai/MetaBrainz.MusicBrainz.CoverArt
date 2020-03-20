using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using JetBrains.Annotations;

using MetaBrainz.Common.Json;
using MetaBrainz.Common.Json.Converters;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;
using MetaBrainz.MusicBrainz.CoverArt.Objects;

namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>Class providing access to the CoverArt Archive API.</summary>
  [PublicAPI]
  public class CoverArt {

    #region Static Fields / Properties

    /// <summary>The default port number to use for requests (-1 to not specify any explicit port).</summary>
    public static int DefaultPort { get; set; } = -1;

    /// <summary>The default user agent to use for requests.</summary>
    public static string DefaultUserAgent { get; set; } = string.Empty;

    /// <summary>The default web site to use for requests.</summary>
    public static string DefaultWebSite { get; set; } = "coverartarchive.org";

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
    public const string UserAgentUrl = "https://github.com/Zastai/MusicBrainz";

    #endregion

    #region Constructors

    private static string CreateUserAgent(string application, string version, string contact) {
      if (string.IsNullOrWhiteSpace(application))
        throw new ArgumentException("The application name must not be blank.", nameof(application));
      return $"{application.Trim()}/{version.Trim()} ({contact.Trim()})";
    }

    /// <summary>Creates a new instance of the <see cref="CoverArt"/> class.</summary>
    /// <param name="userAgent">
    /// The user agent to use for all requests (should be of the form <c>APPLICATION/VERSION (CONTACT)</c>).
    /// </param>
    /// <exception cref="ArgumentException">
    /// When the user agent (whether from <paramref name="userAgent"/> or <see cref="DefaultUserAgent"/>) is blank.
    /// </exception>
    public CoverArt(string? userAgent = null) {
      // libcoverart replaces all dashes by slashes; but that turns valid user agents like "CERN-LineMode/2.15" into invalid ones
      // ("CERN/LineMode/2.15")
      this.UserAgent = userAgent ?? CoverArt.DefaultUserAgent;
      if (string.IsNullOrWhiteSpace(userAgent))
        throw new ArgumentException("The user agent must not be blank.", nameof(userAgent));
      // Simple Defaults
      this.Port = CoverArt.DefaultPort;
      this.WebSite = CoverArt.DefaultWebSite;
      { // Set full user agent, including this library's information
        var an = Assembly.GetExecutingAssembly().GetName();
        this._fullUserAgent = $"{this.UserAgent} {an.Name}/{an.Version} ({CoverArt.UserAgentUrl})";
      }
    }

    /// <summary>Creates a new instance of the <see cref="CoverArt"/> class.</summary>
    /// <param name="application">The application name to use in the user agent property for all requests.</param>
    /// <param name="version">The version number to use in the user agent property for all requests.</param>
    /// <param name="contact">
    /// The contact address (typically HTTP or MAILTO) to use in the user agent property for all requests.
    /// </param>
    /// <exception cref="ArgumentException">When <paramref name="application"/> is blank.</exception>
    public CoverArt(string application, Version version, Uri contact)
    : this(application, version.ToString(), contact.ToString())
    { }

    /// <summary>Creates a new instance of the <see cref="CoverArt"/> class.</summary>
    /// <param name="application">The application name to use in the user agent property for all requests.</param>
    /// <param name="version">The version number to use in the user agent property for all requests.</param>
    /// <param name="contact">
    /// The contact address (typically a URL or email address) to use in the user agent property for all requests.
    /// </param>
    /// <exception cref="ArgumentException">When <paramref name="application"/> is blank.</exception>
    public CoverArt(string application, string version, string contact)
    : this(CoverArt.CreateUserAgent(application, version, contact))
    { }

    #endregion

    #region Instance Fields / Properties

    /// <summary>The port number to use for requests (-1 to not specify any explicit port).</summary>
    public int Port { get; set; }

    /// <summary>The user agent to use for all requests.</summary>
    public string UserAgent { get; }

    /// <summary>The web site to use for requests.</summary>
    public string WebSite { get; set; }

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
      => this.FetchImage("release", mbid, "back", size);

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
      => this.FetchImage("release", mbid, "front", size);

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
      => this.FetchImage("release-group", mbid, "front", size);

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
    public IRelease FetchGroupRelease(Guid mbid) => this.FetchRelease("release-group", mbid);

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
      => this.FetchImage("release", mbid, id, size);

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
    public IRelease FetchRelease(Guid mbid) => this.FetchRelease("release", mbid);

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

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions {
      // @formatter:off
      AllowTrailingCommas         = false,
      IgnoreNullValues            = false,
      IgnoreReadOnlyProperties    = true,
      PropertyNameCaseInsensitive = false,
      WriteIndented               = true,
      // @formatter:on
      Converters = {
        new InterfaceConverter<IThumbnails, Thumbnails>(),
        new ReadOnlyListOfInterfaceConverter<IImage, Image>(),
        new AnyObjectConverter(),
      }
    };

    private readonly string _fullUserAgent;

    private HttpWebResponse PerformRequest(Uri uri) {
      Debug.Print($"[{DateTime.UtcNow}] CAA REQUEST: GET {uri}");
      if (WebRequest.Create(uri) is HttpWebRequest req) {
        req.Accept = "application/json";
        req.Method = "GET";
        req.UserAgent = this._fullUserAgent;
        return (HttpWebResponse) req.GetResponse();
      }
      throw new InvalidOperationException("Only HTTP-compatible URL schemes are supported.");
    }

    private async Task<HttpWebResponse> PerformRequestAsync(Uri uri) {
      Debug.Print($"[{DateTime.UtcNow}] CAA REQUEST: GET {uri}");
      if (WebRequest.Create(uri) is HttpWebRequest req) {
        req.Accept = "application/json";
        req.Method = "GET";
        req.UserAgent = this._fullUserAgent;
        return (HttpWebResponse) await req.GetResponseAsync().ConfigureAwait(false);
      }
      throw new InvalidOperationException("Only HTTP-compatible URL schemes are supported.");
    }

    private CoverArtImage FetchImage(string entity, Guid mbid, string id, CoverArtImageSize size) {
      var suffix = string.Empty;
      if (size != CoverArtImageSize.Original)
        suffix = "-" + ((int) size).ToString(CultureInfo.InvariantCulture);
      var uri = new UriBuilder("http", this.WebSite, this.Port, $"{entity}/{mbid:D}/{id}{suffix}").Uri;
      using var response = this.PerformRequest(uri);
      Debug.Print($"[{DateTime.UtcNow}] => RESPONSE ({response.ContentType}): {response.ContentLength} bytes");
      if (response.ContentLength > CoverArt.MaxImageSize)
        throw new ArgumentException($"The requested image is too large ({response.ContentLength} > {CoverArt.MaxImageSize}).");
      using var stream = response.GetResponseStream();
      if (stream == null)
        throw new WebException("No data received.", WebExceptionStatus.ReceiveFailure);
      var data = new MemoryStream();
      try {
        stream.CopyTo(data);
      }
      catch {
        data.Dispose();
        throw;
      }
      return new CoverArtImage(id, size, response.ContentType, data);
    }

    private async Task<CoverArtImage> FetchImageAsync(string entity, Guid mbid, string id, CoverArtImageSize size) {
      var suffix = string.Empty;
      if (size != CoverArtImageSize.Original)
        suffix = "-" + ((int) size).ToString(CultureInfo.InvariantCulture);
      var uri = new UriBuilder("http", this.WebSite, this.Port, $"{entity}/{mbid:D}/{id}{suffix}").Uri;
      using var response = await this.PerformRequestAsync(uri).ConfigureAwait(false);
      Debug.Print($"[{DateTime.UtcNow}] => RESPONSE ({response.ContentType}): {response.ContentLength} bytes");
      if (response.ContentLength > MaxImageSize)
        throw new ArgumentException($"The requested image is too large ({response.ContentLength} > {MaxImageSize}).");
#if NETSTD_GE_2_1 || NETCORE_GE_3_0
      var stream = response.GetResponseStream();
      await using var _ = stream.ConfigureAwait(false);
#else
      using var stream = response.GetResponseStream();
#endif
      if (stream == null)
        throw new WebException("No data received.", WebExceptionStatus.ReceiveFailure);
      var data = new MemoryStream();
      try {
        await stream.CopyToAsync(data).ConfigureAwait(false);
      }
      catch {
        data.Dispose();
        throw;
      }
      return new CoverArtImage(id, size, response.ContentType, data);
    }

    private IRelease FetchRelease(string entity, Guid mbid) {
      var uri = new UriBuilder("http", this.WebSite, this.Port, $"{entity}/{mbid:D}").Uri;
      using var response = this.PerformRequest(uri);
      Debug.Print($"[{DateTime.UtcNow}] => RESPONSE ({response.ContentType}): {response.ContentLength} bytes");
      using var stream = response.GetResponseStream();
      if (stream == null)
        throw new WebException("No data received.", WebExceptionStatus.ReceiveFailure);
      var characterSet = response.CharacterSet;
      if (string.IsNullOrWhiteSpace(characterSet))
        characterSet = "utf-8";
      var enc = Encoding.GetEncoding(characterSet);
      using var sr = new StreamReader(stream, enc, false, 1024, true);
      var json = sr.ReadToEnd();
      Debug.Print($"[{DateTime.UtcNow}] => JSON: {JsonUtils.Prettify(json)}");
      return JsonUtils.Deserialize<Release>(json, CoverArt.Options);
    }

    private async Task<IRelease> FetchReleaseAsync(string entity, Guid mbid) {
      var uri = new UriBuilder("http", this.WebSite, this.Port, $"{entity}/{mbid:D}").Uri;
      using var response = await this.PerformRequestAsync(uri).ConfigureAwait(false);
      Debug.Print($"[{DateTime.UtcNow}] => RESPONSE ({response.ContentType}): {response.ContentLength} bytes");
#if NETSTD_GE_2_1 || NETCORE_GE_3_0
      var stream = response.GetResponseStream();
      await using var _ = stream.ConfigureAwait(false);
#else
      using var stream = response.GetResponseStream();
#endif
      if (stream == null)
        throw new WebException("No data received.", WebExceptionStatus.ReceiveFailure);
      var characterSet = response.CharacterSet;
      if (string.IsNullOrWhiteSpace(characterSet))
        characterSet = "utf-8";
      var enc = Encoding.GetEncoding(characterSet);
      using var sr = new StreamReader(stream, enc, false, 1024, true);
      var json = await sr.ReadToEndAsync().ConfigureAwait(false);
      Debug.Print($"[{DateTime.UtcNow}] => JSON: {JsonUtils.Prettify(json)}");
      return JsonUtils.Deserialize<Release>(json, CoverArt.Options);
    }

    #endregion

  }

}
