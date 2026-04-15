using TableViewer.Models;

namespace TableViewer.Services;

public class AuthService(IHttpContextAccessor httpContextAccessor)
{
    public bool IsUserAuthenticated()
    {
        return httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }

    public string? GetCurrentUserName()
    {
        return httpContextAccessor.HttpContext?.User?.Identity?.Name;
    }

    public bool CanAccessProtectedView(ViewConfig view)
    {
        return !view.IsProtected || IsUserAuthenticated();
    }
}