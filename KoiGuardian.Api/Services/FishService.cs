using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;

namespace KoiGuardian.Api.Services
{
    public interface IFishService
    {
        Task<FishResponse> CreateFishAsync(FishRequest fishRequest, CancellationToken cancellationToken);
        Task<FishResponse> UpdateFishAsync(FishRequest fishRequest, CancellationToken cancellationToken);

        Task<Fish> GetFishByIdAsync(int koiId, CancellationToken cancellationToken);
    }

    public class FishService : IFishService
    {
        private readonly IRepository<Fish> _fishRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public FishService(IRepository<Fish> fishRepository, IUnitOfWork<KoiGuardianDbContext> unitOfWork)
        {
            _fishRepository = fishRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<FishResponse> CreateFishAsync(FishRequest fishRequest, CancellationToken cancellationToken)
{
    var fishResponse = new FishResponse();
    var existingFish = await _fishRepository.GetAsync(x => x.KoiID.Equals(fishRequest.KoiID), cancellationToken);

    if (existingFish == null)
    {
        var fish = new Fish
        {
            // Do not set KoiID here; let the database generate it
            Name = fishRequest.Name,
            Image = fishRequest.Image,
            Physique = fishRequest.Physique,
            Length = fishRequest.Length,
            Sex = fishRequest.Sex,
            Breeder = fishRequest.Breeder,
            Age = fishRequest.Age,
            Weight = fishRequest.Weight,
            Variety = fishRequest.Variety,
            InPondSince = fishRequest.InPondSince,
            Price = fishRequest.Price
        };

        _fishRepository.Insert(fish);

        // Save changes to the database
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        fishResponse.Status = "201";
        fishResponse.Message = "Fish created successfully.";
    }
    else
    {
        fishResponse.Status = "409";
        fishResponse.Message = "Fish with the given ID already exists.";
    }

    return fishResponse;
}

       

        public async Task<Fish> GetFishByIdAsync(int koiId, CancellationToken cancellationToken)
        {
            return await _fishRepository.GetAsync(x => x.KoiID == koiId, cancellationToken);
        }

        public async Task<FishResponse> UpdateFishAsync(FishRequest fishRequest, CancellationToken cancellationToken)
        {
            var fishResponse = new FishResponse();
            var existingFish = await _fishRepository.GetAsync(x => x.KoiID.Equals(fishRequest.KoiID), cancellationToken);

            if (existingFish != null)
            {
                existingFish.Name = fishRequest.Name;
                existingFish.Image = fishRequest.Image;
                existingFish.Physique = fishRequest.Physique;
                existingFish.Length = fishRequest.Length;
                existingFish.Sex = fishRequest.Sex;
                existingFish.Breeder = fishRequest.Breeder;
                existingFish.Age = fishRequest.Age;
                existingFish.Weight = fishRequest.Weight;
                existingFish.Variety = fishRequest.Variety;
                existingFish.InPondSince = fishRequest.InPondSince;
                existingFish.Price = fishRequest.Price;

                _fishRepository.Update(existingFish);

                // Save changes to the database
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                fishResponse.Status = "200";
                fishResponse.Message = "Fish updated successfully.";
            }
            else
            {
                fishResponse.Status = "404";
                fishResponse.Message = "Fish with the given ID was not found.";
            }

            return fishResponse;
        }
    }

}
