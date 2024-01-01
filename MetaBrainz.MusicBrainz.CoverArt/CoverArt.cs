using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

using JetBrains.Annotations;

using MetaBrainz.Common;

namespace MetaBrainz.MusicBrainz.CoverArt;

/// <summary>Class providing access to the CoverArt Archive API.</summary>
[PublicAPI]
public sealed partial class CoverArt : IDisposable {

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

}
