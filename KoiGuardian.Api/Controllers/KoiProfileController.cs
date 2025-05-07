using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KoiProfileController(IKoiDiseaseService _services) : ControllerBase
    {
        [HttpGet]
        public async Task<List<KoiDiseaseProfile>> getAll()
        {
            return await _services.GetDiseaseProfile();
        }

        [HttpGet("fish")]
        public async Task<List<KoiDiseaseProfile>> getFish(Guid fishId)
        {
            return await _services.GetDisease(fishId);
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<string> CreateKoiDiseaseProfile (DiseaseProfileRequest request)
        {
            return await _services.CreateProfile(request);
        }
        [HttpGet("recomment")]
        public async Task<ActionResult<RecommendResponse>> GetMedicineRecommendations([FromQuery] Guid fishId)
        {
           return await _services.GetMedicineRecommendationsForFish(fishId);
        }

        [HttpGet("active")]
        [Authorize]
        public async Task<List<KoiDiseaseProfile>> GetActiveProfiles()
        {
            return await _services.GetActiveDiseaseProfiles();
        }

        [HttpPut("update/{profileId}")]
        [Authorize]
        public async Task<string> UpdateProfile(Guid profileId, [FromBody] UpdateDiseaseProfileRequest request, CancellationToken cancellationToken)
        {
            return await _services.UpdateProfile(profileId, request, cancellationToken);
        }
    }
}
