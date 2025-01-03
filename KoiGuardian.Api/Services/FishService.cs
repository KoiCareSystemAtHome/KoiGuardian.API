using Azure.Core;
using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.Api.Services
{
    public interface IFishService
    {
        Task<FishResponse> CreateFishAsync(FishRequest fishRequest, CancellationToken cancellationToken);
        Task<FishResponse> UpdateFishAsync(FishRequest fishRequest, CancellationToken cancellationToken);
        Task<Fish> GetFishByIdAsync(Guid koiId, CancellationToken cancellationToken);
        Task<List<FishRerquireParam>> RequireParam(CancellationToken cancellation);
    }

    public class FishService : IFishService
    {
        private readonly IRepository<Fish> _fishRepository;
        private readonly IRepository<Pond> _pondRepository;
        private readonly IRepository<Variety> _varietyRepository;
        private readonly IRepository<ParameterUnit> _parameterUnitRepository;
        private readonly IRepository<RelKoiParameter> _relKoiparameterRepository;

        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public FishService(
            IRepository<Fish> fishRepository,
            IRepository<Pond> pondRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork,
            IRepository<ParameterUnit> parameterUnitRepository,
            IRepository<RelKoiParameter> relKoiparameterRepository,
            IRepository<Variety> varietyRepository)
        {
            _fishRepository = fishRepository;
            _pondRepository = pondRepository;
            _unitOfWork = unitOfWork;
            _varietyRepository = varietyRepository;
            _relKoiparameterRepository = relKoiparameterRepository;
            _parameterUnitRepository = parameterUnitRepository;
        }

        public async Task<FishResponse> CreateFishAsync(FishRequest fishRequest, CancellationToken cancellationToken)
        {
            var requirementsParam = await RequireParam(cancellationToken);

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

            var variety = await _varietyRepository
                .GetAsync(x => x.VarietyName.ToLower() == fishRequest.VarietyName.ToLower(), cancellationToken);
            if (variety == null)
            {
                variety = new Variety() { 
                    VarietyId = Guid.NewGuid(),
                    VarietyName = fishRequest.VarietyName,
                    Description = "",
                 };
                _varietyRepository.Insert(variety);
            }

            var fish = new Fish
            {
                PondID = fishRequest.PondID,
                Name = fishRequest.Name,
                Image = fishRequest.Image,
                Physique = fishRequest.Physique,
                Sex = fishRequest.Sex,
                Breeder = fishRequest.Breeder,
                Age = fishRequest.Age,
                VarietyId = variety.VarietyId,
                InPondSince = fishRequest.InPondSince,
                Price = fishRequest.Price
            };
            
            // xử lý lưu value require từng dòng
            var validValues = fishRequest.RequirementFishParam.Where(u =>
                   requirementsParam.Select(u => u.ParameterUntiID).Contains(u.ParamterUnitID)
                   );

            foreach (var validValue in validValues)
            {
                _relKoiparameterRepository.Insert(new RelKoiParameter()
                {
                    RelKoiParameterID = Guid.NewGuid(),
                    KoiId = fish.KoiID,
                    ParameterUnitID = validValue.ParamterUnitID,
                    CalculatedDate = DateTime.Now,
                    Value = validValue.Value
                });
            }

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

        public async Task<Fish> GetFishByIdAsync(Guid koiId, CancellationToken cancellationToken)
        {
            return await _fishRepository.GetAsync(x => x.KoiID == koiId, cancellationToken);
        }

        public async Task<List<FishRerquireParam>> RequireParam(CancellationToken cancellation)
        {
            return (await _parameterUnitRepository.FindAsync(
               u => u.Parameter.Type == ParameterType.Fish.ToString()
                   && u.IsActive && u.IsStandard && u.ValidUnitl == null,
               u => u.Include(p => p.Parameter),
               cancellationToken: cancellation))
               .Select(u => new FishRerquireParam()
               {
                   ParameterUntiID = u.ParameterUnitID,
                   ParameterName = u.Parameter.Name,
                   UnitName = u.UnitName,
                   WarningLowwer = u.WarningLowwer,
                   WarningUpper = u.WarningUpper,
                   DangerLower = u.DangerLower,
                   DangerUpper = u.DangerUpper,
                   MeasurementInstruction = u.MeasurementInstruction,
               }).ToList();
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

            var variety = await _varietyRepository
                .GetAsync(x => x.VarietyName.ToLower() == fishRequest.VarietyName.ToLower(), cancellationToken);
            if (variety == null)
            {
                variety = new Variety()
                {
                    VarietyId = Guid.NewGuid(),
                    VarietyName = fishRequest.VarietyName,
                    Description = "",
                };
                _varietyRepository.Insert(variety);
            }

            existingFish.PondID = fishRequest.PondID;
            existingFish.Name = fishRequest.Name;
            existingFish.Image = fishRequest.Image;
            existingFish.Physique = fishRequest.Physique;
            existingFish.Sex = fishRequest.Sex;
            existingFish.Breeder = fishRequest.Breeder;
            existingFish.Age = fishRequest.Age;
            existingFish.VarietyId = variety.VarietyId;
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