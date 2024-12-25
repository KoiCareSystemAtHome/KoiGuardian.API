using KoiGuardian.Api.Services;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PackageController(IPackageServices services) : ControllerBase
    {

        [HttpPost("create-package")]
        public async Task<PackageResponse> CreatePackage([FromBody]CreatePackageRequest createPackage,CancellationToken cancellationToken)
        {
            return await services.CreatePackage(createPackage, cancellationToken);
        }
    }
}
