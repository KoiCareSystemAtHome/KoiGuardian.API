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
        [HttpPut("pond-required-param")]
        public async Task<List<PondRerquireParam>> RequireParam( CancellationToken cancellationToken)
        {
            return await services.RequireParam( cancellationToken);
        }
        [HttpPost("create-pond")]
        public async Task<PondResponse> CreatePond(/*[FromBody] */CreatePondRequest createPond, CancellationToken cancellationToken)
        {
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
            return await services.CreatePond(baseUrl, createPond, cancellationToken);
        }

        [HttpPut("update-pond")]
        public async Task<PondResponse> UpdatePond(/*[FromBody] */UpdatePondRequest updatePond, CancellationToken cancellationToken)
        {
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
            return await services.UpdatePond(baseUrl,updatePond, cancellationToken);
        }
    }
}
