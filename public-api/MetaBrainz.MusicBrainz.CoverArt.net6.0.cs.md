# API Reference: MetaBrainz.MusicBrainz.CoverArt

## Assembly Attributes

```cs
[assembly: System.Runtime.InteropServices.ComVisibleAttribute(false)]
[assembly: System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
```

## Namespace: MetaBrainz.MusicBrainz.CoverArt

### Type: CoverArt

```cs
public sealed class CoverArt : System.IDisposable {

  public const int MaxImageSize = 536870912;

  public static readonly System.Diagnostics.TraceSource TraceSource;

  public const string UserAgentUrl = "https://github.com/Zastai/MetaBrainz.MusicBrainz.CoverArt";

  System.Uri BaseUri {
    public get;
  }

  int DefaultPort {
    public static get;
    public static set;
  }

  string DefaultServer {
    public static get;
    public static set;
  }

  string DefaultUrlScheme {
    public static get;
    public static set;
  }

  System.Collections.Generic.IList<System.Net.Http.Headers.ProductInfoHeaderValue> DefaultUserAgent {
    public static get;
  }

  int Port {
    public get;
    public set;
  }

  string Server {
    public get;
    public set;
  }

  string UrlScheme {
    public get;
    public set;
  }

  System.Collections.Generic.IList<System.Net.Http.Headers.ProductInfoHeaderValue> UserAgent {
    public get;
  }

  public CoverArt();

  public CoverArt(params System.Net.Http.Headers.ProductInfoHeaderValue[] userAgent);

  public CoverArt(System.Net.Http.HttpClient client, bool takeOwnership = false);

  public CoverArt(string application, System.Version? version);

  public CoverArt(string application, System.Version? version, System.Uri contact);

  public CoverArt(string application, System.Version? version, string contact);

  public CoverArt(string application, string? version);

  public CoverArt(string application, string? version, System.Uri contact);

  public CoverArt(string application, string? version, string contact);

  public void Close();

  public void ConfigureClient(System.Action<System.Net.Http.HttpClient>? code);

  public void ConfigureClientCreation(System.Func<System.Net.Http.HttpClient>? code);

  public sealed override void Dispose();

  public CoverArtImage FetchBack(System.Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original);

  public System.Threading.Tasks.Task<CoverArtImage> FetchBackAsync(System.Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original, System.Threading.CancellationToken cancellationToken = default);

  public CoverArtImage FetchFront(System.Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original);

  public System.Threading.Tasks.Task<CoverArtImage> FetchFrontAsync(System.Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original, System.Threading.CancellationToken cancellationToken = default);

  public CoverArtImage FetchGroupFront(System.Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original);

  public System.Threading.Tasks.Task<CoverArtImage> FetchGroupFrontAsync(System.Guid mbid, CoverArtImageSize size = CoverArtImageSize.Original, System.Threading.CancellationToken cancellationToken = default);

  public MetaBrainz.MusicBrainz.CoverArt.Interfaces.IRelease FetchGroupRelease(System.Guid mbid);

  public System.Threading.Tasks.Task<MetaBrainz.MusicBrainz.CoverArt.Interfaces.IRelease> FetchGroupReleaseAsync(System.Guid mbid, System.Threading.CancellationToken cancellationToken = default);

  public MetaBrainz.MusicBrainz.CoverArt.Interfaces.IRelease? FetchGroupReleaseIfAvailable(System.Guid mbid);

  public System.Threading.Tasks.Task<MetaBrainz.MusicBrainz.CoverArt.Interfaces.IRelease?> FetchGroupReleaseIfAvailableAsync(System.Guid mbid, System.Threading.CancellationToken cancellationToken = default);

  public CoverArtImage FetchImage(System.Guid mbid, string id, CoverArtImageSize size = CoverArtImageSize.Original);

  public System.Threading.Tasks.Task<CoverArtImage> FetchImageAsync(System.Guid mbid, string id, CoverArtImageSize size = CoverArtImageSize.Original, System.Threading.CancellationToken cancellationToken = default);

  public MetaBrainz.MusicBrainz.CoverArt.Interfaces.IRelease FetchRelease(System.Guid mbid);

  public System.Threading.Tasks.Task<MetaBrainz.MusicBrainz.CoverArt.Interfaces.IRelease> FetchReleaseAsync(System.Guid mbid, System.Threading.CancellationToken cancellationToken = default);

  public MetaBrainz.MusicBrainz.CoverArt.Interfaces.IRelease? FetchReleaseIfAvailable(System.Guid mbid);

  public System.Threading.Tasks.Task<MetaBrainz.MusicBrainz.CoverArt.Interfaces.IRelease?> FetchReleaseIfAvailableAsync(System.Guid mbid, System.Threading.CancellationToken cancellationToken = default);

  protected override void Finalize();

}
```

### Type: CoverArtImage

```cs
public sealed class CoverArtImage : System.IDisposable {

  public readonly string? ContentType;

  public readonly System.IO.Stream Data;

  public readonly string Id;

  public readonly CoverArtImageSize Size;

  [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
  public System.Drawing.Image Decode(bool useEmbeddedColorManagement = false, bool validateImageData = false);

  public sealed override void Dispose();

  protected override void Finalize();

}
```

### Type: CoverArtImageSize

```cs
public enum CoverArtImageSize {

  HugeThumbnail = 1200,
  LargeThumbnail = 500,
  Original = 0,
  SmallThumbnail = 250,

}
```

### Type: CoverArtType

```cs
[System.FlagsAttribute]
public enum CoverArtType : long {

  Back = 2L,
  Booklet = 4L,
  Bottom = 32768L,
  Front = 1L,
  Liner = 256L,
  MatrixRunout = 8192L,
  Medium = 8L,
  None = 0L,
  Obi = 32L,
  Other = -9223372036854775808L,
  Poster = 1024L,
  RawUnedited = 4096L,
  Spine = 64L,
  Sticker = 512L,
  Top = 16384L,
  Track = 128L,
  Tray = 16L,
  Unknown = 4611686018427387904L,
  Watermark = 2048L,

}
```

## Namespace: MetaBrainz.MusicBrainz.CoverArt.Interfaces

### Type: IImage

```cs
public interface IImage : MetaBrainz.Common.Json.IJsonBasedObject {

  bool Approved {
    public abstract get;
  }

  bool Back {
    public abstract get;
  }

  string? Comment {
    public abstract get;
  }

  int Edit {
    public abstract get;
  }

  bool Front {
    public abstract get;
  }

  string Id {
    public abstract get;
  }

  System.Uri? Location {
    public abstract get;
  }

  IThumbnails Thumbnails {
    public abstract get;
  }

  MetaBrainz.MusicBrainz.CoverArt.CoverArtType Types {
    public abstract get;
  }

  System.Collections.Generic.IReadOnlyList<string>? UnknownTypes {
    public abstract get;
  }

}
```

### Type: IRelease

```cs
public interface IRelease : MetaBrainz.Common.Json.IJsonBasedObject {

  System.Collections.Generic.IReadOnlyList<IImage> Images {
    public abstract get;
  }

  System.Uri Location {
    public abstract get;
  }

}
```

### Type: IThumbnails

```cs
public interface IThumbnails : MetaBrainz.Common.Json.IJsonBasedObject {

  System.Uri? Large {
    public abstract get;
  }

  System.Uri? Size1200 {
    public abstract get;
  }

  System.Uri? Size250 {
    public abstract get;
  }

  System.Uri? Size500 {
    public abstract get;
  }

  System.Uri? Small {
    public abstract get;
  }

}
```
