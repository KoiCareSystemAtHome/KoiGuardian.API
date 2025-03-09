using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PondController(IPondServices services) : ControllerBase
    {
        [HttpGet("get-all")]
        public async Task<List<PondDto>> GetAllPondhAsync([FromQuery] string? name = null, CancellationToken cancellationToken = default)
        {
            return await services.GetAllPondhAsync(name,cancellationToken);
        }
        [HttpGet("get-by-owner")]
        public async Task<List<PondDto>> GetAllFishOwnerAsync([FromQuery] Guid owner, CancellationToken cancellationToken = default)
        {
            return await services.GetAllPondByOwnerId(owner, cancellationToken);
        }

        [HttpGet("pond-required-param")]
        public async Task<List<PondRerquireParam>> RequireParam( CancellationToken cancellationToken)
        {
            return await services.RequireParam( cancellationToken);
        }
        [HttpPost("create-pond")]
        public async Task<PondResponse> CreatePond([FromBody] CreatePondRequest createPond, CancellationToken cancellationToken)
        {
            return await services.CreatePond( createPond, cancellationToken);
        }

        [HttpPut("update-pond")]
        public async Task<PondResponse> UpdatePond([FromBody] UpdatePondRequest updatePond, CancellationToken cancellationToken)
        {
            return await services.UpdatePond(updatePond, cancellationToken);
        }

        [HttpGet("get-pond/{pondId}")]
        public async Task<PondDetailResponse> GetPondById(Guid pondId, CancellationToken cancellationToken)
        {
            return await services.GetPondById(pondId, cancellationToken);
        }

        [HttpPut("update-iot-pond")]
        public async Task<PondResponse> UpdateIOTPond([FromBody] UpdatePondIOTRequest updatePond, CancellationToken cancellationToken)
        {
            return await services.UpdateIOTPond(updatePond, cancellationToken);
        }
    }
}
