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
    public class NormfoodController(
        INormFoodService service) : ControllerBase
    {
        [HttpPost]
        [Authorize(Roles = ConstantValue.AdminRole)]
        public async Task<bool> update([FromBody] Guid NormId, float foodPercent, CancellationToken cancellationToken)
        {
            return await service.UpdateNormFood(NormId, foodPercent, cancellationToken);
        }

        [HttpGet]
        public async Task<List<NormFoodAmount>> get(CancellationToken token)
        {
            return  await service.GetAllNormFood(token);
        }
    }
}
