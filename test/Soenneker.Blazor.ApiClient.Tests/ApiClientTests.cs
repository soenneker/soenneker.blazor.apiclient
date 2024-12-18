using Soenneker.Blazor.ApiClient.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;


namespace Soenneker.Blazor.ApiClient.Tests;

[Collection("Collection")]
public class ApiClientTests : FixturedUnitTest
{
    private readonly IApiClient _util;

    public ApiClientTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IApiClient>(true);
    }
}
