using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace MetaBrainz.MusicBrainz.CoverArt {

  /// <summary>Class providing access to the CoverArt Archive API.</summary>
  public class CoverArt {

    #region Static Fields / Properties

    /// <summary>The default port number to use for requests (-1 to not specify any explicit port).</summary>
    public static int DefaultPort { get; set; } = -1;

    /// <summary>The default user agent to use for requests.</summary>
    public static string DefaultUserAgent { get; set; } = null;

    /// <summary>The default web site to use for requests.</summary>
    public static string DefaultWebSite { get; set; } = "coverartarchive.org";

    // TODO: Tune downwards.
    private const int MaxImageSize = int.MaxValue;

    #endregion

    #region Constructors

    /// <summary>Creates a new instance of the <see cref="T:CoverArt"/> class.</summary>
    /// <param name="userAgent">The user agent to use for all requests.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="userAgent"/> is null, and no default was set via <see cref="DefaultUserAgent"/>.</exception>
    public CoverArt(string userAgent = null) {
      this.UserAgent = userAgent ?? CoverArt.DefaultUserAgent;
      if (this.UserAgent == null)
        throw new ArgumentNullException(nameof(userAgent));
      // libcoverart replaces all dashes by slashes; but that turns valid user agents like "CERN-LineMode/2.15" into invalid ones ("CERN/LineMode/2.15")
    }

    #endregion

    #region Instance Fields / Properties

    /// <summary>The port number to use for requests (-1 to not specify any explicit port).</summary>
    public int Port { get; set; } = CoverArt.DefaultPort;

    /// <summary>The user agent to use for all requests.</summary>
    public string UserAgent { get; }

    /// <summary>The web site to use for requests.</summary>
    public string WebSite { get; set; } = CoverArt.DefaultWebSite;

    #endregion

    #region Image Retrieval

    /// <summary>Fetch the main "back" image for the specified release.</summary>
    /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
    /// <param name="size">The requested image size; <see cref="ImageSize.Original"/> can be any content type, but the thumbnails are always JPEG.</param>
    /// <returns>The requested image data.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possibe status codes for the response are:
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
    ///   Possibe status codes for the response are:
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
    ///   Possibe status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release group does not exist (or has no "front" image set);</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public RawImage FetchGroupFront(Guid mbid, ImageSize size = ImageSize.Original) => this.FetchImage("release-group", mbid, "front", size);

    /// <summary>Fetch the specified image for the specified release, in the specified size.</summary>
    /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
    /// <param name="id">The ID of the requested image (as found via <see cref="Image.Id"/>, or "front"/"back" as special case).</param>
    /// <param name="size">The requested image size; <see cref="ImageSize.Original"/> can be any content type, but the thumbnails are always JPEG.</param>
    /// <returns>The requested image data.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possibe status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release and/or the specified image do not exist;</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public RawImage FetchImage(Guid mbid, string id, ImageSize size = ImageSize.Original) => this.FetchImage("release", mbid, id, size);

    private RawImage FetchImage(string entity, Guid mbid, string id, ImageSize size) {
      var suffix = string.Empty;
      if (size != ImageSize.Original)
        suffix = string.Concat("-", ((int) size).ToString(CultureInfo.InvariantCulture));
      var uri = new UriBuilder("http", this.WebSite, this.Port, $"{entity}/{mbid:D}/{id}{suffix}");
      var req = WebRequest.Create(uri.Uri) as HttpWebRequest;
      if (req == null)
        throw new InvalidOperationException("Only HTTP-compatible URL schemes are supported.");
      req.Method    = "GET";
      req.UserAgent = this.UserAgent;
      using (var response = (HttpWebResponse) req.GetResponse()) {
        if (response.ContentLength > CoverArt.MaxImageSize)
          throw new ArgumentException($"Retrieving images larger than {CoverArt.MaxImageSize} is not supported.");
        var bytes = new byte[(int) response.ContentLength];
        using (var stream = response.GetResponseStream())
          stream?.Read(bytes, 0, (int) response.ContentLength);
        return new RawImage(response.ContentType, bytes);
      }
    }

    #endregion

    #region Metadata Retrieval

    /// <summary>Fetch information about the coverart associated with the specified MusicBrainz release group (if any).</summary>
    /// <param name="mbid">The MusicBrainz release group ID for which coverart information is requested.</param>
    /// <returns>A <see cref="Release"/> object containing information about the cover art for the release group's main release.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possibe status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release group does not exist (or has no associated coverart);</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public Release FetchGroupRelease(Guid mbid) => this.FetchRelease("release-group", mbid);

    /// <summary>Fetch information about the coverart associated with the specified MusicBrainz release (if any).</summary>
    /// <param name="mbid">The MusicBrainz release ID for which coverart information is requested.</param>
    /// <returns>A <see cref="Release"/> object containing information about the release's cover art.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possibe status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no associated coverart);</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public Release FetchRelease(Guid mbid) => this.FetchRelease("release", mbid);

    private Release FetchRelease(string entity, Guid mbid) {
      var uri = new UriBuilder("http", this.WebSite, this.Port, $"{entity}/{mbid:D}");
      var req = WebRequest.Create(uri.Uri) as HttpWebRequest;
      if (req == null)
        throw new InvalidOperationException("Only HTTP-compatible URL schemes are supported.");
      req.Accept    = "application/json";
      req.Method    = "GET";
      req.UserAgent = this.UserAgent;
      var json = string.Empty;
      using (var response = (HttpWebResponse) req.GetResponse()) {
        var stream = response.GetResponseStream();
        if (stream != null) {
          var encname = response.CharacterSet;
          if (string.IsNullOrWhiteSpace(encname))
            encname = "utf-8";
          var enc = Encoding.GetEncoding(encname);
          using (var sr = new StreamReader(stream, enc))
            json = sr.ReadToEnd();
        }
      }
      return new Release(new JavaScriptSerializer().Deserialize<JsonObjects.Release>(json));
    }

    #endregion

  }

}
