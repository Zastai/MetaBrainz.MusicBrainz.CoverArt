using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using MetaBrainz.Common;
using MetaBrainz.Common.Json;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;
using MetaBrainz.MusicBrainz.CoverArt.Json;
using MetaBrainz.MusicBrainz.CoverArt.Objects;

namespace MetaBrainz.MusicBrainz.CoverArt;

public sealed partial class CoverArt {

  #region JSON Options

  private static readonly JsonSerializerOptions JsonReaderOptions = JsonUtils.CreateReaderOptions(Converters.Readers);

  #endregion

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
  [GeneratedRegex(@"^(?:.*\n)*\s*<title>(\d+)?\s*(.*?)\s*</title>\s*<h1>\s*(.*?)\s*</h1>\s*<p>\s*(.*?)\s*</p>\s*$")]
  private static partial Regex ErrorResponseContentPattern();

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

}
