using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;

namespace KoiGuardian.Api.Services
{

    public interface IPackageServices
    {
         Task<PackageResponse> CreatePackage(CreatePackageRequest packageRequest, CancellationToken cancellation);
    }

    public class PackageServices(IRepository<Package> packageRepository) : IPackageServices
    {

        public async Task<PackageResponse> CreatePackage(CreatePackageRequest packageRequest, CancellationToken cancellation)
        {
            var packakeResponse = new PackageResponse();
            Package package = null;
            //var package = await packageRepository.GetAsync(x => x.PackageId.Equals(packageRequest.PackageId), cancellation);
            if (package is null) 
            {
                Package newPackage = new()
                {
                    PackageId = packageRequest.PackageId,
                    PackageTitle = packageRequest.PackageTitle,
                    PackageDescription = packageRequest.PackageDescription,
                    PackagePrice = packageRequest.PackagePrice,
                    Type = packageRequest.Type,
                    StartDate = packageRequest.StartDate,
                    EndDate = packageRequest.EndDate,
                };
                //packageRepository.Insert(newPackage);
                packakeResponse.status = "201";
                packakeResponse.message = "Create Package Success";
            }
            else
            {
                packakeResponse.status = "409 ";
                packakeResponse.message = "Package Has Existed";
            }
            return packakeResponse;
        }
    }
}
