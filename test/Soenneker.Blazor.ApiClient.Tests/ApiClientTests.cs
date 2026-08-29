using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Soenneker.Blazor.ApiClient.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Blazor.ApiClient.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class ApiClientTests : HostedUnitTest
{
    private readonly IApiClient _apiClient;

    public ApiClientTests(Host host) : base(host)
    {
        _apiClient = Resolve<IApiClient>(true);
    }

    [Test]
    public void Rejects_insecure_non_loopback_base_address()
    {
        Action act = () => _apiClient.Initialize("http://api.example.com", false);
        Action unsupportedScheme = () => _apiClient.Initialize("ftp://localhost", false);

        act.Should().Throw<InvalidOperationException>();
        unsupportedScheme.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task Rejects_cross_origin_authenticated_request()
    {
        _apiClient.Initialize("https://api.example.com", false);

        Func<Task> act = async () => await _apiClient.Get("https://other.example.com/data");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
