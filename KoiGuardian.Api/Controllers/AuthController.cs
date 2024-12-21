using KoiGuardian.Api.Services;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController
        (IAuthServices service) 
        : ControllerBase
    {
        [HttpPost("login")]
        public async Task<LoginResponse> Login(string username , string password, CancellationToken token)
        {
            return await service.Login(username, password, token);
        }
    }
}
