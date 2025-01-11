using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FishController(IFishService fishService) : ControllerBase
    {
        [HttpGet]
        public async Task<List<Fish>> GetAllFishAsync([FromQuery] string? name = null, CancellationToken cancellationToken = default)
        {
            return await fishService.GetAllFishAsync(name,cancellationToken);
        }

        [HttpPut("pond-required-param")]
        public async Task<List<FishRerquireParam>> RequireParam(CancellationToken cancellationToken)
        {
            return await fishService.RequireParam(cancellationToken);
        }

        [HttpPost("create-fish")]
        public async Task<FishResponse> CreateFishAsync([FromBody] FishRequest fishRequest, CancellationToken cancellationToken)
        {
            return await fishService.CreateFishAsync(fishRequest, cancellationToken);
        }

        [HttpPut("update-fish")]
        public async Task<FishResponse> UpdateFishAsync([FromBody] FishRequest fishRequest, CancellationToken cancellationToken)
        {
            return await fishService.UpdateFishAsync(fishRequest, cancellationToken);
        }

        [HttpGet("{koiId}")]
        public async Task<FishResponse> GetFishByIdAsync(Guid koiId, CancellationToken cancellationToken)
        {
            var fish = await fishService.GetFishByIdAsync(koiId, cancellationToken);
            if (fish == null)
            {
                return new FishResponse
                {
                    Status = "404",
                    Message = $"Fish with ID {koiId} was not found."
                };
            }

            return new FishResponse
            {
                Status = "200",
                Message = "Fish retrieved successfully.",
               
            };
        }
    }
}
