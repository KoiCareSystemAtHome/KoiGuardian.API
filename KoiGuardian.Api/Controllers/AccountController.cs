using Azure;
using KoiGuardian.Api.Services;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using static KoiGuardian.Models.Enums.CommonEnums;
using KoiGuardian.Api.Constants;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController
        (IAccountServices service, IImageUploadService image)
        : ControllerBase
    {

        [HttpPost("login")]
        public async Task<LoginResponse> Login(string username, string password, CancellationToken token)
        {
            return await service.Login(username, password, token);
        }

        [HttpPost("login/google")]
        public async Task<LoginResponse> LoginbyGoogle(string email, CancellationToken token)
        {
            return await service.Login(email, token);
        }

        [HttpPost("register")]
        public async Task<string> Register(RegistrationRequestDto model, [FromBody] IFormFile avatar)
        {
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
            model.Avatar = avatar;
            return await service.Register(baseUrl ,model, CancellationToken.None);
        }

        [HttpGet("dashboard")]
        [Authorize (Roles = ConstantValue.AdminRole)]
        public async Task<AccountDashboardResponse> AdminAccountDashboard(DateTime? startDate, DateTime? endDate)
        {
            return await service.AccountDashboard(startDate, endDate);
        }

        [HttpPut]
        [Authorize(Roles = ConstantValue.AdminRole)]
        public async Task<List<UserDto>> GetAccount([FromBody]AccountFilterRequest request)
        {
            return await service.Filter(request);
        }

        [HttpPut("activate")]
        public async Task<string> ActivateAccount(string email, int code)
        {
            return await service.ActivateAccount(email,code);
        }

        [HttpPut("resend-code")]
        public async Task<bool> ResendCode(string email)
        {
            return await service.ResendCode(email);
        }

        [HttpPost("ForgotPassword")]
        public async Task<string> ForgotPassword(string email)
        {
            return await service.ForgotPassword(email);
        }

        [HttpPost("ConfirmResetPassCode")]
        public async Task<string> ConfirmResetPassCode(string email, int code, string newPass)
        {
            return await service.ConfirmResetPassCode(email, code, newPass);
        }

        [HttpPost("ChangePassword")]
        public async Task<string> ChangePassword(string email, string oldPass, string newPass)
        {
            return await service.ChangePassword(email, oldPass, newPass);
        }

        [HttpPost("Updateprofile")]
        public async Task<string> UpdateProfile(UpdateProfileRequest request)
        {
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";

            return await service.UpdateProfile(baseUrl, request);
        }

        [HttpPost("test")]
        public async Task<string> test(IFormFile filene)
        {
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";

            return await image.UploadImageAsync(baseUrl,"test", "123", filene);
        }
    }
}
