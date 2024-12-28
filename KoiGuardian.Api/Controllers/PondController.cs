using KoiGuardian.Api.Services;
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
        [HttpPost("create-pond")]
        public async Task<PondResponse> CreatePond([FromBody] PondRequest createPond, CancellationToken cancellationToken)
        {
            return await services.CreatePond(createPond, cancellationToken);
        }

        [HttpPut("update-pond")]
        public async Task<PondResponse> UpdatePond([FromBody] PondRequest updatePond, CancellationToken cancellationToken)
        {
            return await services.UpdatePond(updatePond, cancellationToken);
        }
    }
}
