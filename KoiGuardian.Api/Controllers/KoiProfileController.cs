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


    }
}
