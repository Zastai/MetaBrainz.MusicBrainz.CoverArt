using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace MetaBrainz.MusicBrainz.CoverArtArchive {

  /// <summary>Class providing access to the CoverArt Archive API.</summary>
  public class CoverArt {

    #region Static Fields / Properties

    /// <summary>The default port number to use for requests (-1 to not specify any explicit port).</summary>
    public static int DefaultPort { get; set; } = -1;

    /// <summary>The default URL scheme to use for requests.</summary>
    public static string DefaultUrlScheme { get; set; } = "https";

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
      // TODO: Validate/Transform user agent string
    }

    #endregion

    #region Instance Fields / Properties

    /// <summary>The port number to use for requests (-1 to not specify any explicit port).</summary>
    public int Port { get; set; } = CoverArt.DefaultPort;

    /// <summary>The URL scheme to use for requests.</summary>
    public string UrlScheme { get; set; } = CoverArt.DefaultUrlScheme;

    /// <summary>The user agent to use for all requests.</summary>
    public string UserAgent { get; }

    /// <summary>The web site to use for requests.</summary>
    public string WebSite { get; set; } = CoverArt.DefaultWebSite;

    #endregion

    #region Image Retrieval

    public RawImage FetchBack(Guid mbid, ImageSize size = ImageSize.Original) => this.FetchImage(mbid, "back", size);

    public RawImage FetchFront(Guid mbid, ImageSize size = ImageSize.Original) => this.FetchImage(mbid, "front", size);

    public RawImage FetchImage(Guid mbid, string id, ImageSize size = ImageSize.Original) {
      var suffix = string.Empty;
      if (size != ImageSize.Original)
        suffix = string.Concat("-", ((int) size).ToString(CultureInfo.InvariantCulture));
      var uri = new UriBuilder(this.UrlScheme, this.WebSite, this.Port, $"release/{mbid}/{id}{suffix}");
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

    public ReleaseInfo FetchReleaseInfo(Guid mbid) {
      var uri = new UriBuilder(this.UrlScheme, this.WebSite, this.Port, $"release/{mbid}");
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
          var encname = response.ContentEncoding;
          if (string.IsNullOrWhiteSpace(encname))
            encname = "utf-8";
          var enc = Encoding.GetEncoding(encname);
          using (var sr = new StreamReader(stream, enc))
            json = sr.ReadToEnd();
        }
      }
      return new ReleaseInfo(new JavaScriptSerializer().Deserialize<JsonObjects.Release>(json));
    }

    #endregion

  }

}
