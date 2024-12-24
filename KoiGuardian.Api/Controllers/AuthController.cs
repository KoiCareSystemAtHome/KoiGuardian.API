using Azure;
using KoiGuardian.Api.Services;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using static KoiGuardian.Models.Enums.CommonEnums;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController
        (IAuthServices service)
        : ControllerBase
    {
        [HttpPost("login")]
        public async Task<LoginResponse> Login(string username, string password, CancellationToken token)
        {
            return await service.Login(username, password, token);
        }

        [HttpPost("register")]
        [Authorize(Roles =  "Member")]
        public async Task<string> Register([FromBody] RegistrationRequestDto model)
        {
            return await service.Register(model, CancellationToken.None);
        }
    }
}
