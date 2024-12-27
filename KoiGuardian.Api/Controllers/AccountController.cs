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
        (IAccountServices service)
        : ControllerBase
    {
        [HttpPost("login")]
        public async Task<LoginResponse> Login(string username, string password, CancellationToken token)
        {
            return await service.Login(username, password, token);
        }

        [HttpPost("register")]
        public async Task<string> Register([FromBody] RegistrationRequestDto model)
        {
            return await service.Register(model, CancellationToken.None);
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
    }
}
