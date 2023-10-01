using System;
using System.Net;
using System.Net.Http;

using JetBrains.Annotations;

namespace MetaBrainz.MusicBrainz.CoverArt;

/// <summary>An error reported by an HTTP response.</summary>
[PublicAPI]
[Serializable]
public class HttpError : Exception {

  /// <summary>Creates a new HTTP error.</summary>
  /// <param name="response">The response to take the status code and reason from.</param>
  public HttpError(HttpResponseMessage response) : this(response.StatusCode, response.ReasonPhrase) { }

  /// <summary>Creates a new HTTP error.</summary>
  /// <param name="status">The status code for the error.</param>
  /// <param name="reason">The reason phrase associated with the error.</param>
  public HttpError(HttpStatusCode status, string? reason) : base($"HTTP {(int) status}/{status} '{reason}'") {
    this.Status = status;
    this.Reason = reason;
  }

  /// <summary>The reason phrase associated with the error.</summary>
  public string? Reason { get; }

  /// <summary>The status code for the error.</summary>
  public HttpStatusCode Status { get; }

  /// <summary>Gets a textual representation of the HTTP error.</summary>
  /// <returns>A string of the form <c>HTTP nnn/StatusName 'REASON'</c>.</returns>
  public override string ToString() => $"HTTP {(int) this.Status}/{this.Status} '{this.Reason}'";

}
