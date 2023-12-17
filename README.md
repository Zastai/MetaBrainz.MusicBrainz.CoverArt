# MetaBrainz.MusicBrainz.CoverArt [![Build Status][CI-S]][CI-L] [![NuGet Version][NuGet-S]][NuGet-L]

This is a .NET implementation of the libcoverart library (wrapping the
[CoverArtArchive API][api-reference]).
An attempt has been made to keep the same basic class hierarchy.

[CI-S]: https://github.com/Zastai/MetaBrainz.MusicBrainz.CoverArt/actions/workflows/build.yml/badge.svg
[CI-L]: https://github.com/Zastai/MetaBrainz.MusicBrainz.CoverArt/actions/workflows/build.yml

[NuGet-S]: https://img.shields.io/nuget/v/MetaBrainz.MusicBrainz.CoverArt
[NuGet-L]: https://www.nuget.org/packages/MetaBrainz.MusicBrainz.CoverArt

[api-reference]: https://musicbrainz.org/doc/Cover_Art_Archive/API

## Debugging

The `CoverArt` class provides a `TraceSource` that can be used to
configure debug output; its name is `MetaBrainz.MusicBrainz.CoverArt`.

### Configuration

#### In Code

In code, you can enable tracing like follows:

```cs
// Use the default switch, turning it on.
CoverArt.TraceSource.Switch.Level = SourceLevels.All;

// Alternatively, use your own switch so multiple things can be
// enabled/disabled at the same time.
var mySwitch = new TraceSwitch("MyAppDebugSwitch", "All");
CoverArt.TraceSource.Switch = mySwitch;

// By default, there is a single listener that writes trace events to
// the debug output (typically only seen in an IDE's debugger). You can
// add (and remove) listeners as desired.
var listener = new ConsoleTraceListener {
  Name = "MyAppConsole",
  TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ProcessId,
};
CoverArt.TraceSource.Listeners.Clear();
CoverArt.TraceSource.Listeners.Add(listener);
```

#### In Configuration

Starting from .NET 7 your application can also be set up to read tracing
configuration from the application configuration file. To do so, the
application needs to add the following to its startup code:

```cs
System.Diagnostics.TraceConfiguration.Register();
```

(Provided by the `System.Configuration.ConfigurationManager` package.)

The application config file can then have a `system.diagnostics` section
where sources, switches and listeners can be configured.

```xml
<configuration>
  <system.diagnostics>
    <sharedListeners>
      <add name="console" type="System.Diagnostics.ConsoleTraceListener" traceOutputOptions="DateTime,ProcessId" />
    </sharedListeners>
    <sources>
      <source name="MetaBrainz.MusicBrainz.CoverArt" switchName="MetaBrainz.MusicBrainz.CoverArt">
        <listeners>
          <add name="console" />
          <add name="caa-log" type="System.Diagnostics.TextWriterTraceListener" initializeData="caa.log" />
        </listeners>
      </source>
    </sources>
    <switches>
      <add name="MetaBrainz.MusicBrainz.CoverArt" value="All" />
    </switches>
  </system.diagnostics>
</configuration>
```

## Release Notes

These are available [on GitHub][release-notes].

[release-notes]: https://github.com/Zastai/MetaBrainz.MusicBrainz.CoverArt/releases
