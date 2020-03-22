# MetaBrainz.MusicBrainz.CoverArt [![Build Status](https://img.shields.io/appveyor/build/zastai/metabrainz-musicbrainz-coverart)](https://ci.appveyor.com/project/Zastai/metabrainz-musicbrainz-coverart) [![NuGet Version](https://img.shields.io/nuget/v/MetaBrainz.MusicBrainz.CoverArt)](https://www.nuget.org/packages/MetaBrainz.MusicBrainz.CoverArt)

This is a .NET implementation of the libcoverart library (wrapping the [CoverArtArchive API](https://musicbrainz.org/doc/Cover_Art_Archive/API)).
An attempt has been made to keep the same basic class hierarchy.

## Release Notes

### v2.0 (2020-03-21)

- Target .NET Standard 2.0 and 2.1, .NET Core 2.1 and 3.1 (the current LTS releases) and .NET Framework 4.6.1, 4.7.2 and 4.8.
- Renamed `RawImage` to `CoverArtImage` and `ImageSize` to `CoverArtImageSize`.
- Switched to `System.Text.Json` (instead of `NewtonSoft.Json`).
- Use `MetaBrainz.Common.Json`.
- Use `System.Drawing.Common` to provide image decoding on all targets.
- Split up the three JSON-based classes (`Release`, `Image`, and `Thumbnails`)
  - interfaces (with I prefix) in an `Interfaces` namespace
    - all derive from `IJsonBasedObject`, which catches all unsupported JSON properties
  - classes, now internal, in an `Objects` namespace
- Minor doc fixes.
- Minor internal tweaks (e.g. use async streams where available).
- Use nullable reference types.

### v1.1.1 (2018-11-15)

Corrected the build so that the IntelliSense XML documentation is property built and packaged.

### v1.1 (2018-08-14)

Minor API updates.

- [STYLE-980](https://tickets.metabrainz.org/browse/STYLE-980): Handle Raw/Unedited image type.
- [CAA-88](https://tickets.metabrainz.org/browse/CAA-88): Adjusted thumbnail types. Dropped `Huge` (huge); added `Size250` (250), `Size500` (500) and `Size1200` (1200).
- Bumped `Newtonsoft.Json` to the latest version (11.0.2).

### v1.0 (2018-01-21)

First official release.

- Dropped support for .NET framework versions before 4.0 (and 4.0 may be dropped in a later version); this allows for builds using .NET Core (which cannot target 2.0/3.5).
- Added support for .NET Standard 2.0; the only unsupported API is RawImage.Decode() (because System.Drawing.Image is not available).
