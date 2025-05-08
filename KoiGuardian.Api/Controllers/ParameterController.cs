using KoiGuardian.Api.Constants;
using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParameterController : ControllerBase
    {
        private readonly IParameterService _parameterService;

        public ParameterController(IParameterService parameterService)
        {
            _parameterService = parameterService;
        }

        [HttpPost]
        [Route("upsert-from-excel")]
        [Authorize(Roles = ConstantValue.AdminRole)]
        public async Task<IActionResult> UpsertParametersFromExcel(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "Invalid file." });

            var result = await _parameterService.UpsertFromExcel(file, cancellationToken);

            if (result.status == "200")
                return Ok(result);

            return BadRequest(result);
        }

        [HttpGet]
        [Route("getAll")]
        public async Task<IActionResult> GetAll(Guid parameterId, CancellationToken cancellationToken)
        {
           

            var result = await _parameterService.getAll(parameterId, cancellationToken);

            return Ok(result);
        }

        [HttpGet("type")]
        public async Task<List<object>> Get(string type, CancellationToken cancellationToken)
        {
            return await _parameterService.getAll(type, cancellationToken);
        }

        [HttpPost("edit-pond-param")]
        [Authorize(Roles = ConstantValue.AdminRole)]
        public async Task<string> EditPondParam(PondStandardParam type, CancellationToken cancellationToken)
        {
            return await _parameterService.EditPondParam(type, cancellationToken);
        }


        [HttpPost("edit-fish-param")]
        [Authorize(Roles = ConstantValue.AdminRole)]
        public async Task<string> EditFishParam(KoiStandardParam type, CancellationToken cancellationToken)
        {
            return await _parameterService.EditFishParam(type, cancellationToken);
        }
    }
}
