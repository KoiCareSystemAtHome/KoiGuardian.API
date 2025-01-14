using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.Api.Services
{

    public interface IPackageServices
    {
         Task<PackageResponse> CreatePackage(CreatePackageRequest packageRequest, CancellationToken cancellation);
        Task<IEnumerable<Package>> GetAllPackageAsync(CancellationToken cancellationToken);
    }

    public class PackageServices(IRepository<Package> packageRepository,  KoiGuardianDbContext _dbContext) : IPackageServices
    {

        public async Task<PackageResponse> CreatePackage(CreatePackageRequest packageRequest, CancellationToken cancellation)
        {
            var packakeResponse = new PackageResponse();
            var package = await packageRepository.GetAsync(x => x.PackageTitle.Equals(packageRequest.PackageTitle), cancellation);
            if (package is null) 
            {
                package = new Package()
                {
                    PackageId = Guid.NewGuid(),
                    PackageTitle = packageRequest.PackageTitle,
                    PackageDescription = packageRequest.PackageDescription,
                    PackagePrice = packageRequest.PackagePrice,
                    Type = packageRequest.Type,
                    StartDate = packageRequest.StartDate,
                    EndDate = packageRequest.EndDate,
                };
                packageRepository.Insert(package);
                await _dbContext.SaveChangesAsync(cancellation);

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

        public async Task<IEnumerable<Package>> GetAllPackageAsync(CancellationToken cancellationToken)
        {
            return await packageRepository
                .GetQueryable()
                .AsNoTracking()   
                .ToListAsync(cancellationToken);
        }
    }
}
