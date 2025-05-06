using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;

namespace KoiGuardian.Api.Services
{
    public interface INormFoodService
    {
        Task<bool> UpdateNormFood(Guid NormId, float foodPercent, CancellationToken cancellationToken);
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
            return (await normFoodRepo.GetAllAsync()).OrderBy(u => u.AgeFrom).ThenBy(u => u.AgeTo).ToList();
        }

        public async Task<bool> UpdateNormFood(Guid NormId, float foodPercent, CancellationToken cancellationToken)
        {
            try
            {
                var norm = await normFoodRepo.FindAsync( u => u.NormFoodAmountId == NormId);
                var normFoodAmount = norm.FirstOrDefault();
                if (normFoodAmount == null)
                {
                    return false;
                }
                normFoodAmount.StandardAmount = foodPercent/100;

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