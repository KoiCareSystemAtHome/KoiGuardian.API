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
    public class PackageController(IPackageServices services) : ControllerBase
    {

        [HttpPost("create-package")]
        public async Task<PackageResponse> CreatePackage([FromBody]CreatePackageRequest createPackage,CancellationToken cancellationToken)
        {
            return await services.CreatePackage(createPackage, cancellationToken);
        }

        [HttpGet]
        public async Task<IEnumerable<Package>> GetAllPackageAsync(CancellationToken cancellationToken)
        {
            return await services.GetAllPackageAsync(cancellationToken);
        }

        [HttpPost("update-package")]
        public async Task<PackageResponse> Update([FromBody] UpdatePackageRequest createPackage, CancellationToken cancellationToken)
        {
            return await services.UpdatePackage(createPackage, cancellationToken);
        }

        [HttpGet("filter")]
        public async Task<IEnumerable<Package>> FilterPackageAsync(FilterPackageRequest request, CancellationToken cancellationToken)
        {
            return await services.Filter(request, cancellationToken);
        }
    }
}
