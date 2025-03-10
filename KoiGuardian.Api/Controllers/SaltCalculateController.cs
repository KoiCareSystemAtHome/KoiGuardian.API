using Microsoft.AspNetCore.Mvc;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using KoiGuardian.Api.Services;
using System;
using KoiGuardian.DataAccess.Db;

namespace KoiGuardian.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SaltCalculateController : ControllerBase
    {
        private readonly ISaltCalculatorService _saltCalculatorService;

        public SaltCalculateController(ISaltCalculatorService saltCalculatorService)
        {
            _saltCalculatorService = saltCalculatorService;
        }
        [HttpPost("calculate")]
        public async Task<CalculateSaltResponse> CalculateSalt([FromBody] CalculateSaltRequest request)
        {
            return await _saltCalculatorService.CalculateSalt(request);
        }
        [HttpPost("addition-process")]
        public async Task<SaltAdditionProcessResponse> GetSaltAdditionProcess([FromQuery] Guid pondId)
        {
            return await _saltCalculatorService.GetSaltAdditionProcess(pondId);
        }

        [HttpPost("notifications")]
        public async Task<List<Notification>> GetSaltNotifications([FromQuery] Guid pondId)
        {
            return await _saltCalculatorService.GetSaltNotifications(pondId);
        }
        [HttpPost("adjust-start-time")]
        public async Task<bool> AdjustSaltAdditionStartTime([FromBody] AdjustSaltStartTimeRequest request)
        {
            return await _saltCalculatorService.AdjustSaltAdditionStartTime(
                request.PondId,
                request.NewStartTime);
        }


    }
}
