using Microsoft.AspNetCore.Components;

namespace Soenneker.Blazor.ApiClient.Tests;

internal sealed class TestNavigationManager : NavigationManager
{
    public TestNavigationManager()
    {
        Initialize("https://localhost/", "https://localhost/");
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
    }
}
