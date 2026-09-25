using System.Text.Json.Serialization;

namespace Soenneker.Blazor.ApiClient.Tests;

[JsonSerializable(typeof(string))]
internal partial class TestJsonSerializerContext : JsonSerializerContext
{
}
