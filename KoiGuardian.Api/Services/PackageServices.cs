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
         Task<PackageResponse> UpdatePackage(UpdatePackageRequest packageRequest, CancellationToken cancellation);
        Task<IEnumerable<Package>> GetAllPackageAsync(CancellationToken cancellationToken);
        Task<IEnumerable<Package>> Filter(FilterPackageRequest request,CancellationToken cancellationToken);
    }

    public class PackageServices
        (IRepository<Package> packageRepository,  KoiGuardianDbContext _dbContext) : IPackageServices
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

        public async Task<IEnumerable<Package>> Filter(FilterPackageRequest request, CancellationToken cancellationToken)
        {
            var raw =  packageRepository
                .GetQueryable()
                .AsNoTracking();
            if (string.IsNullOrEmpty (request.SeachKey))
            {
                raw = raw.Where(u => u.PackageTitle.Contains(request.SeachKey)
                || u.PackageDescription.Contains(request.SeachKey));
            }

            if (string.IsNullOrEmpty(request.Type))
            {
                raw = raw.Where(u => u.Type == request.Type);
            }

            if (request.StartDate != null)
            {
                raw = raw.Where(u => u.StartDate >= request.StartDate);
            }

            if (request.EndDate != null)
            {
                raw = raw.Where(u => u.EndDate <= request.EndDate);
            }

            return raw;
        }

        public async Task<IEnumerable<Package>> GetAllPackageAsync(CancellationToken cancellationToken)
        {
            return await packageRepository
                .GetQueryable()
                .AsNoTracking()   
                .ToListAsync(cancellationToken);
        }

        public async Task<PackageResponse> UpdatePackage(UpdatePackageRequest packageRequest, CancellationToken cancellation)
        {
            var packakeResponse = new PackageResponse();
            var package = await packageRepository.GetAsync(x => x.PackageId.Equals(packageRequest.PackageId), cancellation);
            if (package is null)
            {
                packakeResponse.status = "404";
                packakeResponse.message = "Package not found!";
            }
            else
            {
                package.PackageTitle = packageRequest.PackageTitle;
                package.PackageDescription = packageRequest.PackageDescription;
                package.PackagePrice = packageRequest.PackagePrice;
                package.Type = packageRequest.Type;
                package.StartDate = packageRequest.StartDate;   
                package.EndDate = packageRequest.EndDate;

                packakeResponse.status = "200";
                packakeResponse.message = "Package update success";

                packageRepository.Update(package);
                await _dbContext.SaveChangesAsync();
            }
            return packakeResponse;
        }
    }
}
