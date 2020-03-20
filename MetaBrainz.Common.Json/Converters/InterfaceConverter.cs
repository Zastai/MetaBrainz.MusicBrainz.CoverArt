using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetaBrainz.Common.Json.Converters {

  /// <summary>A JSON converter to handle an interface (or base class) using a specific implementation or subclass.</summary>
  /// <typeparam name="TInterface">The interface (or base class) type of the property being converted to/from JSON.</typeparam>
  /// <typeparam name="TObject">The specific implementation type to use.</typeparam>
  public sealed class InterfaceConverter<TInterface, TObject> : JsonConverter<TInterface?> where TInterface : class where TObject : class, TInterface {

      /// <inheritdoc />
  public override TInterface? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      return JsonSerializer.Deserialize<TObject?>(ref reader, options);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TInterface? value, JsonSerializerOptions options) {
      JsonSerializer.Serialize(writer, (TObject?) value, options);
    }

  }

}
