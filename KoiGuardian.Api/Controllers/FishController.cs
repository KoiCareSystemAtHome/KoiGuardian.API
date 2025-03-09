using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FishController(
        IFishService fishService,
        IKoiDiseaseService _services) : ControllerBase
    {
        [HttpGet]
        public async Task<List<FishDetailResponse>> GetAllFishAsync([FromQuery] string? name = null, CancellationToken cancellationToken = default)
        {
            return await fishService.GetAllFishAsync(name,cancellationToken);
        }

        [HttpGet("get-by-owner")]
        public async Task<List<FishDto>> GetAllFishOwnerAsync([FromQuery] Guid owner , CancellationToken cancellationToken = default)
        {
            return await fishService.GetFishByOwnerId(owner, cancellationToken);
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
        public async Task<FishDetailResponse> GetFishByIdAsync(Guid koiId, CancellationToken cancellationToken)
        {
           return await fishService.GetFishByIdAsync(koiId, cancellationToken);
           
        }
    }
}
