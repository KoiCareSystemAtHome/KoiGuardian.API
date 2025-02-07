using Azure.Core;
using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace KoiGuardian.Api.Services
{
    public interface IFishService
    {
        Task<FishResponse> CreateFishAsync(FishRequest fishRequest, CancellationToken cancellationToken);
        Task<FishResponse> UpdateFishAsync(FishRequest fishRequest, CancellationToken cancellationToken);
        Task<Fish> GetFishByIdAsync(Guid koiId, CancellationToken cancellationToken);
        Task<List<FishRerquireParam>> RequireParam(CancellationToken cancellation);
        Task<List<FishDto>> GetFishByOwnerId(Guid Owner,CancellationToken cancellation);
        Task<List<Fish>> GetAllFishAsync(string? name = null, CancellationToken cancellationToken = default);

    }

    public class FishService : IFishService
    {
        private readonly IRepository<Fish> _fishRepository;
        private readonly IRepository<Pond> _pondRepository;
        private readonly IRepository<Variety> _varietyRepository;
        private readonly IRepository<Parameter> _parameterRepository;
        private readonly IRepository<RelKoiParameter> _relKoiparameterRepository;
        private readonly IImageUploadService _imageUploadService;

        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public FishService(
            IRepository<Fish> fishRepository,
            IRepository<Pond> pondRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork,
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
            _parameterRepository = parameterRepository;
            _imageUploadService = imageUpload;
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
                variety = new Variety()
                {
                    VarietyId = Guid.NewGuid(),
                    VarietyName = fishRequest.VarietyName,
                    Description = "",
                };
                _varietyRepository.Insert(variety);
            }

            var fish = new Fish
            {
                KoiID = Guid.NewGuid(),
                PondID = fishRequest.PondID,
                Name = fishRequest.Name,
                Physique = fishRequest.Physique,
                Sex = fishRequest.Sex,
                Breeder = fishRequest.Breeder,
                Age = fishRequest.Age,
                VarietyId = variety.VarietyId,
                InPondSince = fishRequest.InPondSince,
                Price = fishRequest.Price,
                Image = fishRequest.Image,

            };



            _fishRepository.Insert(fish);


            // xử lý lưu value require từng dòng
            var validValues = fishRequest.RequirementFishParam.Where(u =>
                   requirementsParam.Select(u => u.HistoryID).Contains(u.ParameterHistoryId)
                   );

            foreach (var validValue in validValues)
            {
                _relKoiparameterRepository.Insert(new RelKoiParameter()
                {
                    RelKoiParameterID = Guid.NewGuid(),
                    KoiId = fish.KoiID,
                    ParameterHistoryId = validValue.ParameterHistoryId,
                    CalculatedDate = DateTime.UtcNow,
                    Value = validValue.Value
                });
            }


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
            var list = (await _parameterRepository.FindAsync(
               u => u.Type.ToLower() == ParameterType.Fish.ToString().ToLower() 
               && u.IsActive && u.ValidUntil == null))
               .Select(u => new FishRerquireParam()
               {
                   HistoryID = u.HistoryId,
                   ParameterName = u.Name,
                   UnitName = u.UnitName,
                   WarningLowwer = u.WarningLowwer,
                   WarningUpper = u.WarningUpper,
                   DangerLower = u.DangerLower,
                   DangerUpper = u.DangerUpper,
                   MeasurementInstruction = u.MeasurementInstruction,

               }).ToList();
            return list;
        }

        public async Task<FishResponse> UpdateFishAsync(FishRequest fishRequest, CancellationToken cancellationToken)
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

            existingFish.Image = fishRequest.Image;

            var validValues = fishRequest.RequirementFishParam.Where(u =>
                   requirementsParam.Select(u => u.HistoryID).Contains(u.ParameterHistoryId)
                   );

            foreach (var validValue in validValues)
            {
                _relKoiparameterRepository.Insert(new RelKoiParameter()
                {
                    RelKoiParameterID = Guid.NewGuid(),
                    KoiId = existingFish.KoiID,
                    ParameterHistoryId = validValue.ParameterHistoryId,
                    CalculatedDate = DateTime.UtcNow,
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

        public async Task<List<Fish>> GetAllFishAsync(string? name = null, CancellationToken cancellationToken = default)
        {
            return (await _fishRepository.FindAsync(
                predicate: !string.IsNullOrWhiteSpace(name)
                    ? f => f.Name.ToLower().Contains(name.ToLower())
                    : null,
                include: query => query
                    .Include(f => f.Variety)
                    .Include(f => f.Pond),
                orderBy: query => query.OrderBy(f => f.Name),
                cancellationToken: cancellationToken
            )).ToList();
        }

        public async Task<List<FishDto>> GetFishByOwnerId(Guid ownerId, CancellationToken cancellationToken = default)
        {
            var fishEntities = await _fishRepository.FindAsync(
                predicate: x => x.Pond.OwnerId.Equals(ownerId.ToString()),
                include: query => query
                    .Include(f => f.Variety)
                    .Include(f => f.Pond),
                orderBy: query => query.OrderBy(f => f.Name),
                cancellationToken: cancellationToken
            );

            // Ánh xạ sang DTO
            var fishDtos = fishEntities.Select(f => new FishDto
            {
                KoiID = f.KoiID,
                Name = f.Name,
                Image = f.Image,
                Price = f.Price,
                Sex = f.Sex,
                Age = f.Age,
                Pond = f.Pond == null ? null : new PondDto
                {
                    PondID = f.Pond.PondID,
                    Name = f.Pond.Name,
                    OwnerId = f.Pond.OwnerId,
                    CreateDate = f.Pond.CreateDate,
                    Image = f.Pond.Image
                },
                Variety = f.Variety == null ? null : new VarietyDto
                {
                    VarietyId = f.Variety.VarietyId,
                    VarietyName = f.Variety.VarietyName,
                    Description = f.Variety.Description,
                    AuthorId = f.Variety.AuthorId
                }
            }).ToList();

            return fishDtos;
        }

    }
}