using System.Text.Json.Serialization;
using Vaerktojer.LogSearch.Data;

namespace Vaerktojer.LogSearch.Lib;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(SearchResult))]
public partial class AppJsonSerializerContext : JsonSerializerContext { }
