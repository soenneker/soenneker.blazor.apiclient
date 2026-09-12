using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Soenneker.Blazor.ApiClient.Tests;

internal sealed class TestAccessTokenProvider : IAccessTokenProvider
{
    public ValueTask<AccessTokenResult> RequestAccessToken() =>
        throw new InvalidOperationException("Token acquisition is not expected in these tests.");

    public ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options) =>
        throw new InvalidOperationException("Token acquisition is not expected in these tests.");
}
