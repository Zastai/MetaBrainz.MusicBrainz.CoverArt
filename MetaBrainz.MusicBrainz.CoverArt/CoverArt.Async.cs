﻿#if NETFX_GE_4_5 // HttpWebRequest only has GetResponseAsync in v4.5 and up

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace MetaBrainz.MusicBrainz.CoverArt {

  [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  public partial class CoverArt {

    #region Public Methods

    /// <summary>Fetch the main "back" image for the specified release.</summary>
    /// <param name="mbid">The MusicBrainz release ID for which the image is requested.</param>
    /// <param name="size">The requested image size; <see cref="ImageSize.Original"/> can be any content type, but the thumbnails are always JPEG.</param>
    /// <returns>An asynchronous operation returning the requested image data.</returns>
    /// <exception cref="WebException">
    ///   When something went wrong with the request. More details can be found in the exception's <see cref="WebException.Response"/> property.<br/>
    ///   Possibe status codes for the response are:
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
    ///   Possibe status codes for the response are:
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
    ///   Possibe status codes for the response are:
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
    ///   Possibe status codes for the response are:
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
    ///   Possibe status codes for the response are:
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
    ///   Possibe status codes for the response are:
    ///   <ul>
    ///     <li>404 (<see cref="HttpStatusCode.NotFound"/>) when the release does not exist (or has no associated coverart);</li>
    ///     <li>503 (<see cref="HttpStatusCode.ServiceUnavailable"/>) when the server is unavailable, or rate limiting is in effect.</li>
    ///   </ul>
    /// </exception>
    public Task<Release> FetchReleaseAsync(Guid mbid) => this.FetchReleaseAsync("release", mbid);

    #endregion

    #region Internals

    private async Task<RawImage> FetchImageAsync(string entity, Guid mbid, string id, ImageSize size) {
      var suffix = string.Empty;
      if (size != ImageSize.Original)
        suffix = string.Concat("-", ((int) size).ToString(CultureInfo.InvariantCulture));
      var uri = new UriBuilder("http", this.WebSite, this.Port, $"{entity}/{mbid:D}/{id}{suffix}");
      var req = WebRequest.Create(uri.Uri) as HttpWebRequest;
      if (req == null)
        throw new InvalidOperationException("Only HTTP-compatible URL schemes are supported.");
      req.Method    = "GET";
      req.UserAgent = this._fullUserAgent;
      using (var response = (HttpWebResponse) await req.GetResponseAsync().ConfigureAwait(false)) {
          if (response.ContentLength > CoverArt.MaxImageSize)
            throw new ArgumentException($"The requested image is too large ({response.ContentLength} > {CoverArt.MaxImageSize}).");
        using (var stream = response.GetResponseStream()) {
          if (stream == null)
            return null;
          var reader = new BinaryReader(stream);
          if (response.ContentLength != -1) {
            var data = reader.ReadBytes((int) response.ContentLength);
            // FIXME: What if data.Length does not match response.ContentLength?
            return new RawImage(response.ContentType, data);
          }
          const int chunksize = 8 * 1024;
          using (var data = new MemoryStream()) {
            var chunk = reader.ReadBytes(chunksize);
            while (chunk.Length == chunksize) {
              data.Write(chunk, 0, chunk.Length);
              if (data.Length > CoverArt.MaxImageSize) {
                chunk = null;
                break;
              }
              chunk = reader.ReadBytes(chunksize);
            }
            if (chunk != null && chunk.Length != 0)
              data.Write(chunk, 0, chunk.Length);
            if (data.Length > CoverArt.MaxImageSize)
              throw new ArgumentException($"The requested image is too large ({data.Length} > {CoverArt.MaxImageSize}).");
            return new RawImage(response.ContentType, data.ToArray());
          }
        }
      }
    }

    private async Task<Release> FetchReleaseAsync(string entity, Guid mbid) {
      var uri = new UriBuilder("http", this.WebSite, this.Port, $"{entity}/{mbid:D}");
      var req = WebRequest.Create(uri.Uri) as HttpWebRequest;
      if (req == null)
        throw new InvalidOperationException("Only HTTP-compatible URL schemes are supported.");
      req.Accept    = "application/json";
      req.Method    = "GET";
      req.UserAgent = this._fullUserAgent;
      var json = string.Empty;
      using (var response = (HttpWebResponse) await req.GetResponseAsync().ConfigureAwait(false)) {
        var stream = response.GetResponseStream();
        if (stream != null) {
          var encname = response.CharacterSet;
          if (encname == null || encname.Trim().Length == 0)
            encname = "utf-8";
          var enc = Encoding.GetEncoding(encname);
          using (var sr = new StreamReader(stream, enc))
            json = sr.ReadToEnd();
        }
      }
      return JsonConvert.DeserializeObject<Release>(json, CoverArt.SerializerSettings);
    }

    #endregion

  }

}

#endif
