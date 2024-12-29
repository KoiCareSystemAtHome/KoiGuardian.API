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
        private readonly IRepository<Pond> _pondRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public FishService(
            IRepository<Fish> fishRepository,
            IRepository<Pond> pondRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork)
        {
            _fishRepository = fishRepository;
            _pondRepository = pondRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<FishResponse> CreateFishAsync(FishRequest fishRequest, CancellationToken cancellationToken)
        {
            var fishResponse = new FishResponse();

            // Check if the fish already exists
            var existingFish = await _fishRepository.GetAsync(x => x.KoiID.Equals(fishRequest.KoiID), cancellationToken);
            if (existingFish != null)
            {
                fishResponse.Status = "409";
                fishResponse.Message = "Fish with the given ID already exists.";
                return fishResponse;
            }

            // Verify that the specified pond exists
            var pond = await _pondRepository.GetAsync(x => x.PondID.Equals(fishRequest.PondID), cancellationToken);
            if (pond == null)
            {
                fishResponse.Status = "404";
                fishResponse.Message = "Specified pond does not exist.";
                return fishResponse;
            }

            var fish = new Fish
            {
                PondID = fishRequest.PondID,
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

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                fishResponse.Status = "201";
                fishResponse.Message = "Fish created successfully.";
            }
            catch (Exception ex)
            {
                fishResponse.Status = "500";
                fishResponse.Message = "Error creating fish: " + ex.Message;
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
            if (existingFish == null)
            {
                fishResponse.Status = "404";
                fishResponse.Message = "Fish with the given ID was not found.";
                return fishResponse;
            }

            // Verify that the specified pond exists if pond is being changed
            if (existingFish.PondID != fishRequest.PondID)
            {
                var newPond = await _pondRepository.GetAsync(x => x.PondID.Equals(fishRequest.PondID), cancellationToken);
                if (newPond == null)
                {
                    fishResponse.Status = "404";
                    fishResponse.Message = "Specified pond does not exist.";
                    return fishResponse;
                }
            }

            existingFish.PondID = fishRequest.PondID;
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

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                fishResponse.Status = "200";
                fishResponse.Message = "Fish updated successfully.";
            }
            catch (Exception ex)
            {
                fishResponse.Status = "500";
                fishResponse.Message = "Error updating fish: " + ex.Message;
            }

            return fishResponse;
        }
    }
}