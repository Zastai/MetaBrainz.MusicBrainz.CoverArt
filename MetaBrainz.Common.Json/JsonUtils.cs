using System;
using System.Text;
using System.Text.Json;

namespace MetaBrainz.Common.Json {

  /// <summary>Utility class, providing various methods to ease the use of System.Text.Json.</summary>
  public static class JsonUtils {

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions {
      // @formatter:off
      AllowTrailingCommas         = false,
      IgnoreNullValues            = false,
      IgnoreReadOnlyProperties    = true,
      PropertyNameCaseInsensitive = false,
      WriteIndented               = true,
      // @formatter:on
    };

    private static string DecodeUtf8(ReadOnlySpan<byte> bytes) {
#if NETSTD_GE_2_1 || NETCORE_GE_2_1
      return Encoding.UTF8.GetString(bytes);
#else
      return Encoding.UTF8.GetString(bytes.ToArray());
#endif
    }

    /// <summary>Deserializes JSON to an object of the specified type, using default options.</summary>
    /// <param name="json">The JSON to deserialize.</param>
    /// <typeparam name="T">The type of object to deserialize.</typeparam>
    /// <returns>A newly deserialized object of type <typeparamref name="T"/>.</returns>
    public static T Deserialize<T>(string json) {
      return JsonSerializer.Deserialize<T>(json, JsonUtils.Options);
    }

    /// <summary>Deserializes JSON to an object of the specified type, using default options.</summary>
    /// <param name="json">The JSON to deserialize.</param>
    /// <param name="options">The options to use for deserialization.</param>
    /// <typeparam name="T">The type of object to deserialize.</typeparam>
    /// <returns>A newly deserialized object of type <typeparamref name="T"/>.</returns>
    public static T Deserialize<T>(string json, JsonSerializerOptions options) {
      return JsonSerializer.Deserialize<T>(json, options);
    }

    /// <summary>Decodes the current raw JSON value as a string.</summary>
    /// <param name="reader">The UTF-8 JSON reader to get the raw value from.</param>
    /// <returns>The raw value as a string.</returns>
    public static string GetRawStringValue(this ref Utf8JsonReader reader) {
      var value = "";
      if (reader.HasValueSequence) {
        foreach (var memory in reader.ValueSequence)
          value += JsonUtils.DecodeUtf8(memory.Span);
      }
      else
        value = JsonUtils.DecodeUtf8(reader.ValueSpan);
      return value;
    }

    /// <summary>Pretty-prints a JSON string.</summary>
    /// <param name="json">The JSON string to pretty-print.</param>
    /// <returns>An indented version of <paramref name="json"/>.</returns>
    public static string Prettify(string json) {
      try {
        return JsonSerializer.Serialize(JsonDocument.Parse(json).RootElement, JsonUtils.Options);
      }
      catch {
        return json;
      }
    }

  }

}
