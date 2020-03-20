using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetaBrainz.Common.Json.Converters {

  /// <summary>
  /// A JSON converter to handle a read-only list containing an interface (or base class) using a list containing a specific
  /// implementation or subclass.
  /// </summary>
  /// <typeparam name="TInterface">
  /// The interface (or base class) type stored in the read-only list type of the property being converted to/from JSON.
  /// </typeparam>
  /// <typeparam name="TObject">The specific implementation type to use for the actual backing list.</typeparam>
  public sealed class ReadOnlyListOfInterfaceConverter<TInterface, TObject> : JsonConverter<IReadOnlyList<TInterface>?> where TInterface : class where TObject : class, TInterface {

    /// <inheritdoc />
    public override IReadOnlyList<TInterface>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      var objects = JsonSerializer.Deserialize<List<TObject>>(ref reader, options);
      var interfaces = objects?.Select(o => (TInterface) o).ToList();
      return interfaces;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IReadOnlyList<TInterface>? interfaces, JsonSerializerOptions options) {
      if (interfaces == null)
        return;
      var objects = interfaces.Select(i => (TObject) i).ToList();
      JsonSerializer.Serialize(writer, objects, options);
    }

  }

}
