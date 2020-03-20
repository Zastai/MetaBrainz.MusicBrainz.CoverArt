using System.Collections.Generic;
using System.Text.Json.Serialization;

using JetBrains.Annotations;

namespace MetaBrainz.Common.Json {

  /// <summary>A JSON-based object, holding any properties not handled by normal deserialization.</summary>
  [PublicAPI]
  public abstract class JsonBasedObject : IJsonBasedObject {

    IReadOnlyDictionary<string, object?>? IJsonBasedObject.UnhandledProperties => this.UnhandledProperties;

    /// <inheritdoc cref="IJsonBasedObject.UnhandledProperties"/>
    /// <remarks>This is only public because System.Text.Json requires it.</remarks>
    [JsonExtensionData]
    public Dictionary<string, object?>? UnhandledProperties { get; set; }

  }

}
