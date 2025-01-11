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
        Task<FishResponse> CreateFishAsync(string baseUrl, FishRequest fishRequest, CancellationToken cancellationToken);
        Task<FishResponse> UpdateFishAsync(string baseUrl, FishRequest fishRequest, CancellationToken cancellationToken);
        Task<Fish> GetFishByIdAsync(Guid koiId, CancellationToken cancellationToken);
        Task<List<FishRerquireParam>> RequireParam(CancellationToken cancellation);
    }

    public class FishService : IFishService
    {
        private readonly IRepository<Fish> _fishRepository;
        private readonly IRepository<Pond> _pondRepository;
        private readonly IRepository<Variety> _varietyRepository;
        private readonly IRepository<ParameterUnit> _parameterUnitRepository;
        private readonly IRepository<Parameter> _parameterRepository;
        private readonly IRepository<RelKoiParameter> _relKoiparameterRepository;
        private readonly IImageUploadService _imageUploadService;

        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public FishService(
            IRepository<Fish> fishRepository,
            IRepository<Pond> pondRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork,
            IRepository<ParameterUnit> parameterUnitRepository,
            IRepository<Parameter> parameterRepository,
            IRepository<RelKoiParameter> relKoiparameterRepository,
            IRepository<Variety> varietyRepository,
            IImageUploadService imageUpload)
        {
            _fishRepository = fishRepository;
            _pondRepository = pondRepository;
            _unitOfWork = unitOfWork;
            _varietyRepository = varietyRepository;
            _relKoiparameterRepository = relKoiparameterRepository;
            _parameterUnitRepository = parameterUnitRepository;
            _parameterRepository = parameterRepository;
            _imageUploadService = imageUpload;
        }

        public async Task<FishResponse> CreateFishAsync(string baseUrl, FishRequest fishRequest, CancellationToken cancellationToken)
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
                Physique = fishRequest.Physique,
                Sex = fishRequest.Sex,
                Breeder = fishRequest.Breeder,
                Age = fishRequest.Age,
                VarietyId = variety.VarietyId,
                InPondSince = fishRequest.InPondSince,
                Price = fishRequest.Price
            };

            var image = await _imageUploadService.UploadImageAsync(baseUrl, "Fish", fish.KoiID.ToString(), fishRequest.Image);
            fish.Image = image;

            // xử lý lưu value require từng dòng
            var validValues = fishRequest.RequirementFishParam.Where(u =>
                   requirementsParam.SelectMany(u => u.ParameterUnits?.Select( u => u.ParameterUntiID)).Contains(u.ParamterUnitID)
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
            return (await _parameterRepository.FindAsync(
               u => u.Type == ParameterType.Fish.ToString(),
               u => u.Include(p => p.ParameterUnits.Where( u => u.IsActive && u.IsStandard && u.ValidUnitl == null)),
               cancellationToken: cancellation))
               .Select(u => new FishRerquireParam()
               {
                   ParameterID = u.ParameterID,
                   ParameterName = u.Name,
                   ParameterUnits = u.ParameterUnits.Select(
                       u => new FishRerquireParamUnit()
                       {
                           ParameterUntiID = u.ParameterUnitID,
                           UnitName = u.UnitName,
                           WarningLowwer = u.WarningLowwer,
                           WarningUpper = u.WarningUpper,
                           DangerLower = u.DangerLower,
                           DangerUpper = u.DangerUpper,
                           MeasurementInstruction = u.MeasurementInstruction,
                       }).ToList()
                   
               }).ToList();
        }

        public async Task<FishResponse> UpdateFishAsync(string baseUrl, FishRequest fishRequest, CancellationToken cancellationToken)
        {
            var requirementsParam = await RequireParam(cancellationToken);
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
            existingFish.Physique = fishRequest.Physique;
            existingFish.Sex = fishRequest.Sex;
            existingFish.Breeder = fishRequest.Breeder;
            existingFish.Age = fishRequest.Age;
            existingFish.VarietyId = variety.VarietyId;
            existingFish.InPondSince = fishRequest.InPondSince;
            existingFish.Price = fishRequest.Price;
            var image = await _imageUploadService.UploadImageAsync(baseUrl, "Fish", existingFish.KoiID.ToString(), fishRequest.Image);
            existingFish.Image = image;

            var validValues = fishRequest.RequirementFishParam.Where(u =>
                   requirementsParam.SelectMany(u => u.ParameterUnits?.Select(u => u.ParameterUntiID)).Contains(u.ParamterUnitID)
                   );

            foreach (var validValue in validValues)
            {
                _relKoiparameterRepository.Insert(new RelKoiParameter()
                {
                    RelKoiParameterID = Guid.NewGuid(),
                    KoiId = existingFish.KoiID,
                    ParameterUnitID = validValue.ParamterUnitID,
                    CalculatedDate = DateTime.Now,
                    Value = validValue.Value
                });
            }

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