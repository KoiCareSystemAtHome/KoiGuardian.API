using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;

namespace KoiGuardian.Api.Services
{
    public interface INormFoodService
    {
        Task<bool> UpdateNormFood(NormFoodAmount normFoodAmount, CancellationToken cancellationToken);
        Task<List<NormFoodAmount>> GetAllNormFood(CancellationToken cancellationToken);

    }

    public class NormFoodService(
        IRepository<NormFoodAmount> normFoodRepo, 
        IUnitOfWork<KoiGuardianDbContext> uom
        )
        : INormFoodService
    {
        public async Task<List<NormFoodAmount>> GetAllNormFood(CancellationToken cancellationToken)
        {
            return await normFoodRepo.GetAllAsync();
        }

        public async Task<bool> UpdateNormFood(NormFoodAmount normFoodAmount, CancellationToken cancellationToken)
        {
            try
            {
                normFoodRepo.Update(normFoodAmount);
                await uom.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}