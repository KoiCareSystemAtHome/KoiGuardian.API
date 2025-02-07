using Microsoft.AspNetCore.Mvc;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using KoiGuardian.Api.Services;
using System;

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
        public async Task<IActionResult> CalculateSalt([FromBody] CalculateSaltRequest request)
        {
           

            var response = await _saltCalculatorService.CalculateSalt(request);
            return Ok(new
            {
                Success = true,
                PondId = response.PondId,
                TotalSalt = response.TotalSalt,
                Message = "Tính toán thành công.",
                AdditionalInstructions = response.AdditionalInstruction
            });
        }

        [HttpPost("validate")]
        public ActionResult<AddSaltResponse> ValidateSaltAddition(AddSaltRequest request)
        {
            var response = _saltCalculatorService.ValidateAndCalculateSaltAddition(request);
            return Ok(response);
        }

        [HttpPost("add")]
        public ActionResult AddSalt(AddSaltRequest request)
        {
            var validation = _saltCalculatorService.ValidateAndCalculateSaltAddition(request);

            if (!validation.CanAddSalt)
            {
                return BadRequest(new
                {
                    Messages = validation.Messages,
                    NextAllowedTime = validation.NextAllowedTime
                });
            }

            _saltCalculatorService.RecordSaltAddition(request.PondId, validation.AllowedSaltWeightKg);

            return Ok(new
            {
                PondId = request.PondId,
                AddedAmount = validation.AllowedSaltWeightKg,
                CurrentSaltLevel = _saltCalculatorService.GetCurrentSaltWeightKg(request.PondId),
                Messages = validation.Messages
            });
        }

        [HttpGet("current/{pondId}")]
        public ActionResult GetCurrentSaltLevel(Guid pondId)
        {
            var currentLevel = _saltCalculatorService.GetCurrentSaltWeightKg(pondId);
            return Ok(new { PondId = pondId, CurrentSaltLevel = currentLevel });
        }

        [HttpGet("remaining-time/{pondId}/{saltAmountToAdd}")]
        public ActionResult GetRemainingTimeToAddSalt(Guid pondId, double saltAmountToAdd)
        {
            var remainingTime = _saltCalculatorService.CalculateRemainingTimeToAddSalt(pondId, saltAmountToAdd);
            return Ok(new
            {
                Success = true,
                PondId = pondId,
                RemainingTime = remainingTime.ToString(@"d' days 'hh\:mm\:ss") // Return in hours:minutes:seconds format
            });
        }
    }
}
