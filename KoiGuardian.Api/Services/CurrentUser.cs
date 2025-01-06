using System.Security.Claims;

namespace KoiGuardian.Api.Services;

public interface ICurrentUser
{
    string UserName();

    string Rolename(); 

}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserName()
    {
        var username = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
        return username ?? string.Empty;
    }

    public string Rolename()
    {
        var role = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
        return role ?? string.Empty;
    }
}