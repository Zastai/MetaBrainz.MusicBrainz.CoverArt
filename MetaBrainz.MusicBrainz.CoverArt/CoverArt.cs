#if NETFX_GE_4_5 || NETSTD_GE_2_0 || NETCORE_GE_2_0
# define INCLUDE_ASYNC
#endif

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>Class providing access to the CoverArt Archive API.</summary>
  [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  public class CoverArt {

    #region Static Fields / Properties

    static CoverArt() {
      // Mono's C# compiler does not like initializers on auto-properties, so set them up here instead.
      CoverArt.DefaultPort      = -1;
      CoverArt.DefaultUserAgent = null;
      CoverArt.DefaultWebSite   = "coverartarchive.org";
    }

    /// <summary>The default port number to use for requests (-1 to not specify any explicit port).</summary>
    public static int DefaultPort { get; set; }

    /// <summary>The default user agent to use for requests.</summary>
    public static string DefaultUserAgent { get; set; }

    /// <summary>The default web site to use for requests.</summary>
    public static string DefaultWebSite { get; set; }

    /// <summary>The maximum allowed image size; an exception is thrown if a response larger than this is received from the CoverArt Archive.</summary>
    /// <remarks>
    /// The CoverArt does not actually impose a file size limit.
    /// At the moment, the largest item in the CAA is a PDF of 236MiB, followed by a PNG of 159MiB (<a href="http://notlob.eu/caa/largeimages">source</a>).
    /// Setting the limit at 512MiB therefore seems fairly sensible.
    /// </remarks>
    public const int MaxImageSize = 512 * 1024 * 1024;

    /// <summary>The URL included in the user agent for requests as part of this library's information.</summary>
    public const string UserAgentUrl = "https://github.com/Zastai/MusicBrainz";

    #endregion

    #region Constructors

    /// <summary>Creates a new instance of the <see cref="T:CoverArt"/> class.</summary>
    /// <param name="userAgent">The user agent to use for all requests.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="userAgent"/> is null, and no default was set via <see cref="DefaultUserAgent"/>.</exception>
    /// <exception cref="ArgumentException">When the user agent (whether from <paramref name="userAgent"/> or <see cref="DefaultUserAgent"/>) is blank.</exception>
    public CoverArt(string userAgent = null) {
      // libcoverart replaces all dashes by slashes; but that turns valid user agents like "CERN-LineMode/2.15" into invalid ones ("CERN/LineMode/2.15")
      this.UserAgent = userAgent ?? CoverArt.DefaultUserAgent;
      if (this.UserAgent == null) throw new ArgumentNullException(nameof(userAgent));
      if (this.UserAgent.Trim().Length == 0) throw new ArgumentException("The user agent must not be blank.", nameof(userAgent));
      // Simple Defaults
      this.Port      = CoverArt.DefaultPort;
      this.WebSite   = CoverArt.DefaultWebSite;
      { // Set full user agent, including this library's information
        var an = Assembly.GetExecutingAssembly().GetName();
        this._fullUserAgent = $"{this.UserAgent} {an.Name}/{an.Version} ({CoverArt.UserAgentUrl})";
      }
    }

    /// <summary>Creates a new instance of the <see cref="T:CoverArt"/> class.</summary>
    /// <param name="application">The applciation name to use in the user agent property for all requests.</param>
    /// <param name="version">The version number to use in the user agent property for all requests.</param>
    /// <param name="contact">The contact address (typically HTTP or MAILTO) to use in the user agent property for all requests.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="application"/>, <paramref name="version"/> and/or <paramref name="contact"/> are null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="application"/> is blank.</exception>
    public CoverArt(string application, Version version, Uri contact) {
      if (application == null) throw new ArgumentNullException(nameof(application));
      if (version     == null) throw new ArgumentNullException(nameof(version));
      if (contact     == null) throw new ArgumentNullException(nameof(contact));
      if (application.Trim().Length == 0) throw new ArgumentException("The application name must not be blank.", nameof(application));
      this.UserAgent = $"{application}/{version} ({contact})";
      // Simple Defaults
      this.Port      = CoverArt.DefaultPort;
      this.WebSite   = CoverArt.DefaultWebSite;
      { // Set full user agent, including this library's information
        var an = Assembly.GetExecutingAssembly().GetName();
        this._fullUserAgent = $"{this.UserAgent} {an.Name}/{an.Version} ({CoverArt.UserAgentUrl})";
      }
    }

    /// <summary>Creates a new instance of the <see cref="T:CoverArt"/> class.</summary>
    /// <param name="application">The applciation name to use in the user agent property for all requests.</param>
    /// <param name="version">The version number to use in the user agent property for all requests.</param>
    /// <param name="contact">The contact address (typically a URL or email address) to use in the user agent property for all requests.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="application"/>, <paramref name="version"/> and/or <paramref name="contact"/> are null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="application"/>, <paramref name="version"/> and/or <paramref name="contact"/> are blank.</exception>
    public CoverArt(string application, string version, string contact) {
      if (application == null) throw new ArgumentNullException(nameof(application));
      if (version     == null) throw new ArgumentNullException(nameof(version));
      if (contact     == null) throw new ArgumentNullException(nameof(contact));
      if (application.Trim().Length == 0) throw new ArgumentException("The application name must not be blank.", nameof(application));
      if (version    .Trim().Length == 0) throw new ArgumentException("The version number must not be blank.",   nameof(version));
      if (contact    .Trim().Length == 0) throw new ArgumentException("The contact address must not be blank.",  nameof(contact));
      this.UserAgent = $"{application}/{version} ({contact})";
      // Simple Defaults
      this.Port      = CoverArt.DefaultPort;
      this.WebSite   = CoverArt.DefaultWebSite;
      { // Set full user agent, including this library's information
        var an = Assembly.GetExecutingAssembly().GetName();
        this._fullUserAgent = $"{this.UserAgent} {an.Name}/{an.Version} ({CoverArt.UserAgentUrl})";
      }
    }

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
    /// <param name="size">The requested image size; <see cref="ImageSize.Original"/> can be any content type, but the thumbnails are always JPEG.</param>
    /// <returns>The requested image data.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possible status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no "back" image set);</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public RawImage FetchBack(Guid mbid, ImageSize size = ImageSize.Original)  => this.FetchImage("release", mbid, "back", size);

    /// <summary>Fetch the main "front" image for the specified release, in the specified size.</summary>
    /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
    /// <param name="size">The requested image size; <see cref="ImageSize.Original"/> can be any content type, but the thumbnails are always JPEG.</param>
    /// <returns>The requested image data.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possible status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no "front" image set);</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public RawImage FetchFront(Guid mbid, ImageSize size = ImageSize.Original) => this.FetchImage("release", mbid, "front", size);

    /// <summary>Fetch the main "front" image for the specified release groupt, in the specified size.</summary>
    /// <param name="mbid">The MusicBrainz release group ID for which the image is requested.</param>
    /// <param name="size">The requested image size; <see cref="ImageSize.Original"/> can be any content type, but the thumbnails are always JPEG.</param>
    /// <returns>The requested image data.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possible status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release group does not exist (or has no "front" image set);</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public RawImage FetchGroupFront(Guid mbid, ImageSize size = ImageSize.Original) => this.FetchImage("release-group", mbid, "front", size);

    /// <summary>Fetch information about the coverart associated with the specified MusicBrainz release group (if any).</summary>
    /// <param name="mbid">The MusicBrainz release group ID for which coverart information is requested.</param>
    /// <returns>A <see cref="Release"/> object containing information about the cover art for the release group's main release.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possible status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release group does not exist (or has no associated coverart);</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public Release FetchGroupRelease(Guid mbid) => this.FetchRelease("release-group", mbid);

    /// <summary>Fetch the specified image for the specified release, in the specified size.</summary>
    /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
    /// <param name="id">The ID of the requested image (as found via <see cref="Image.Id"/>, or "front"/"back" as special case).</param>
    /// <param name="size">The requested image size; <see cref="ImageSize.Original"/> can be any content type, but the thumbnails are always JPEG.</param>
    /// <returns>The requested image data.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possible status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release and/or the specified image do not exist;</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public RawImage FetchImage(Guid mbid, string id, ImageSize size = ImageSize.Original) => this.FetchImage("release", mbid, id, size);

    /// <summary>Fetch information about the coverart associated with the specified MusicBrainz release (if any).</summary>
    /// <param name="mbid">The MusicBrainz release ID for which coverart information is requested.</param>
    /// <returns>A <see cref="Release"/> object containing information about the release's cover art.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possible status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no associated coverart);</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public Release FetchRelease(Guid mbid) => this.FetchRelease("release", mbid);

    #endregion

    #region Internals

    private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings {
      CheckAdditionalContent = true,
      MissingMemberHandling  = MissingMemberHandling.Error
    };

    private readonly string _fullUserAgent;

    private HttpWebResponse PerformRequest(Uri uri) {
      Debug.Print($"[{DateTime.UtcNow}] CAA REQUEST: GET {uri}");
      var req = WebRequest.Create(uri) as HttpWebRequest;
      if (req == null)
        throw new InvalidOperationException("Only HTTP-compatible URL schemes are supported.");
      req.Accept    = "application/json";
      req.Method    = "GET";
      req.UserAgent = this._fullUserAgent;
      return (HttpWebResponse) req.GetResponse();
    }

    private RawImage FetchImage(string entity, Guid mbid, string id, ImageSize size) {
      var suffix = string.Empty;
      if (size != ImageSize.Original)
        suffix = "-" + ((int) size).ToString(CultureInfo.InvariantCulture);
      var uri = new UriBuilder("http", this.WebSite, this.Port, $"{entity}/{mbid:D}/{id}{suffix}").Uri;
      using (var response = this.PerformRequest(uri)) {
        Debug.Print($"[{DateTime.UtcNow}] => RESPONSE ({response.ContentType}): {response.ContentLength} bytes");
        if (response.ContentLength > CoverArt.MaxImageSize)
          throw new ArgumentException($"The requested image is too large ({response.ContentLength} > {CoverArt.MaxImageSize}).");
        using (var stream = response.GetResponseStream()) {
          if (stream == null)
            return null;
          var data = new MemoryStream();
          try {
            stream.CopyTo(data);
          }
          catch {
            data.Dispose();
            throw;
          }
          return new RawImage(response.ContentType, data);
        }
      }
    }

    private Release FetchRelease(string entity, Guid mbid) {
      var uri = new UriBuilder("http", this.WebSite, this.Port, $"{entity}/{mbid:D}").Uri;
      using (var response = this.PerformRequest(uri)) {
        var stream = response.GetResponseStream();
        if (stream == null)
          throw new WebException("No data received.", WebExceptionStatus.ReceiveFailure);
        var encname = response.CharacterSet;
        if (encname == null || encname.Trim().Length == 0)
          encname = "utf-8";
        var enc = Encoding.GetEncoding(encname);
        using (var sr = new StreamReader(stream, enc)) {
          var json = sr.ReadToEnd();
          Debug.Print($"[{DateTime.UtcNow}] => RESPONSE ({response.ContentType}): <<\n{JsonConvert.DeserializeObject(json)}\n>>");
          return JsonConvert.DeserializeObject<Release>(json, CoverArt.SerializerSettings);
        }
      }

    }

    #endregion

    #if INCLUDE_ASYNC

    #region Instance Methods (Async)

    /// <summary>Fetch the main "back" image for the specified release.</summary>
    /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
    /// <param name="size">The requested image size; <see cref="ImageSize.Original"/> can be any content type, but the thumbnails are always JPEG.</param>
    /// <returns>An asynchronous operation returning the requested image data.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possible status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no "back" image set);</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public Task<RawImage> FetchBackAsync(Guid mbid, ImageSize size = ImageSize.Original)  => this.FetchImageAsync("release", mbid, "back", size);

    /// <summary>Fetch the main "front" image for the specified release, in the specified size.</summary>
    /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
    /// <param name="size">The requested image size; <see cref="ImageSize.Original"/> can be any content type, but the thumbnails are always JPEG.</param>
    /// <returns>An asynchronous operation returning the requested image data.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possible status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no "front" image set);</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public Task<RawImage> FetchFrontAsync(Guid mbid, ImageSize size = ImageSize.Original) => this.FetchImageAsync("release", mbid, "front", size);

    /// <summary>Fetch the main "front" image for the specified release groupt, in the specified size.</summary>
    /// <param name="mbid">The MusicBrainz release group ID for which the image is requested.</param>
    /// <param name="size">The requested image size; <see cref="ImageSize.Original"/> can be any content type, but the thumbnails are always JPEG.</param>
    /// <returns>An asynchronous operation returning the requested image data.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possible status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release group does not exist (or has no "front" image set);</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public Task<RawImage> FetchGroupFrontAsync(Guid mbid, ImageSize size = ImageSize.Original) => this.FetchImageAsync("release-group", mbid, "front", size);

    /// <summary>Fetch information about the coverart associated with the specified MusicBrainz release group (if any).</summary>
    /// <param name="mbid">The MusicBrainz release group ID for which coverart information is requested.</param>
    /// <returns>
    ///   An asynchronous operation returning a  <see cref="Release"/> object containing information about the cover art for the release group's main release.
    /// </returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possible status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release group does not exist (or has no associated coverart);</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public Task<Release> FetchGroupReleaseAsync(Guid mbid) => this.FetchReleaseAsync("release-group", mbid);

    /// <summary>Fetch the specified image for the specified release, in the specified size.</summary>
    /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
    /// <param name="id">The ID of the requested image (as found via <see cref="Image.Id"/>, or "front"/"back" as special case).</param>
    /// <param name="size">The requested image size; <see cref="ImageSize.Original"/> can be any content type, but the thumbnails are always JPEG.</param>
    /// <returns>An asynchronous operation returning the requested image data.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possible status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release and/or the specified image do not exist;</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public Task<RawImage> FetchImageAsync(Guid mbid, string id, ImageSize size = ImageSize.Original) => this.FetchImageAsync("release", mbid, id, size);

    /// <summary>Fetch information about the coverart associated with the specified MusicBrainz release (if any).</summary>
    /// <param name="mbid">The MusicBrainz release ID for which coverart information is requested.</param>
    /// <returns>An asynchronous operation returning a <see cref="Release"/> object containing information about the release's cover art.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possible status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no associated coverart);</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public Task<Release> FetchReleaseAsync(Guid mbid) => this.FetchReleaseAsync("release", mbid);

    #endregion

    #region Internals (Async)

    private async Task<HttpWebResponse> PerformRequestAsync(Uri uri) {
      Debug.Print($"[{DateTime.UtcNow}] CAA REQUEST: GET {uri}");
      var req = WebRequest.Create(uri) as HttpWebRequest;
      if (req == null)
        throw new InvalidOperationException("Only HTTP-compatible URL schemes are supported.");
      req.Accept    = "application/json";
      req.Method    = "GET";
      req.UserAgent = this._fullUserAgent;
      return (HttpWebResponse) await req.GetResponseAsync().ConfigureAwait(false);
    }

    private async Task<RawImage> FetchImageAsync(string entity, Guid mbid, string id, ImageSize size) {
      var suffix = string.Empty;
      if (size != ImageSize.Original)
        suffix = "-" + ((int) size).ToString(CultureInfo.InvariantCulture);
      var uri = new UriBuilder("http", this.WebSite, this.Port, $"{entity}/{mbid:D}/{id}{suffix}").Uri;
      using (var response = await this.PerformRequestAsync(uri).ConfigureAwait(false)) {
        Debug.Print($"[{DateTime.UtcNow}] => RESPONSE ({response.ContentType}): {response.ContentLength} bytes");
        if (response.ContentLength > CoverArt.MaxImageSize)
          throw new ArgumentException($"The requested image is too large ({response.ContentLength} > {CoverArt.MaxImageSize}).");
        using (var stream = response.GetResponseStream()) {
          if (stream == null)
            return null;
          var data = new MemoryStream();
          try {
            await stream.CopyToAsync(data).ConfigureAwait(false);
          }
          catch {
            data.Dispose();
            throw;
          }
          return new RawImage(response.ContentType, data);
        }
      }
    }

    private async Task<Release> FetchReleaseAsync(string entity, Guid mbid) {
      var uri = new UriBuilder("http", this.WebSite, this.Port, $"{entity}/{mbid:D}").Uri;
      using (var response = await this.PerformRequestAsync(uri).ConfigureAwait(false)) {
        var stream = response.GetResponseStream();
        if (stream == null)
          throw new WebException("No data received.", WebExceptionStatus.ReceiveFailure);
        var encname = response.CharacterSet;
        if (encname == null || encname.Trim().Length == 0)
          encname = "utf-8";
        var enc = Encoding.GetEncoding(encname);
        using (var sr = new StreamReader(stream, enc)) {
          var json = await sr.ReadToEndAsync().ConfigureAwait(false);
          Debug.Print($"[{DateTime.UtcNow}] => RESPONSE ({response.ContentType}): <<\n{JsonConvert.DeserializeObject(json)}\n>>");
          return JsonConvert.DeserializeObject<Release>(json, CoverArt.SerializerSettings);
        }
      }
      
    }

    #endregion

    #endif

  }

}
