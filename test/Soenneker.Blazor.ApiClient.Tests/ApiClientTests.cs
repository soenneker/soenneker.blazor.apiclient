using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.Blazor.ApiClient.Tests;

[Collection("Collection")]
public class ApiClientTests : FixturedUnitTest
{
    public ApiClientTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
    }

    [Fact]
    public void Default()
    {

    }
}
