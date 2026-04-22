using Soenneker.Tests.HostedUnit;

namespace Soenneker.Blazor.ApiClient.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class ApiClientTests : HostedUnitTest
{
    public ApiClientTests(Host host) : base(host)
    {
    }

    [Test]
    public void Default()
    {

    }
}
