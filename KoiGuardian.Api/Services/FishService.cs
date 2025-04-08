using Azure.Core;
using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace KoiGuardian.Api.Services
{
    public interface IFishService
    {
        Task<FishResponse> CreateFishAsync(FishRequest fishRequest, CancellationToken cancellationToken);
        Task<FishResponse> UpdateFishAsync(FishRequest fishRequest, CancellationToken cancellationToken);
        Task<FishDetailResponse> GetFishByIdAsync(Guid koiId, CancellationToken cancellationToken);
        Task<bool> AddNote(Guid koiId, string note);
        Task<List<FishDto>> GetFishByOwnerId(Guid Owner,CancellationToken cancellation);
        Task<List<FishDetailResponse>> GetAllFishAsync(string? name = null, CancellationToken cancellationToken = default);

    }

    public class FishService : IFishService
    {
        private readonly IRepository<Fish> _fishRepository;
        private readonly IRepository<Pond> _pondRepository;
        private readonly IRepository<Variety> _varietyRepository;
        private readonly IRepository<KoiStandardParam> _koiParameterRepository;
        private readonly IRepository<KoiReport> _relKoiparameterRepository;
        private readonly IImageUploadService _imageUploadService;

        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public FishService(
            IRepository<Fish> fishRepository,
            IRepository<Pond> pondRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork,
            IRepository<KoiStandardParam> parameterRepository,
            IRepository<KoiReport> relKoiparameterRepository,
            IRepository<Variety> varietyRepository,
            IImageUploadService imageUpload)
        {
            _fishRepository = fishRepository;
            _pondRepository = pondRepository;
            _unitOfWork = unitOfWork;
            _varietyRepository = varietyRepository;
            _relKoiparameterRepository = relKoiparameterRepository;
            _koiParameterRepository = parameterRepository;
            _imageUploadService = imageUpload;
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

            var variety = await _varietyRepository
                .GetAsync(x => x.VarietyName.ToLower() == fishRequest.VarietyName.ToLower(), cancellationToken);
            if (variety == null)
            {
                variety = new Variety()
                {
                    VarietyId = Guid.NewGuid(),
                    VarietyName = fishRequest.VarietyName,
                    Description = "",
                    AuthorId = " "
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
                Notes = "[]"

            };



            _fishRepository.Insert(fish);


            // xử lý lưu value require từng dòng
            
            
                _relKoiparameterRepository.Insert(new KoiReport()
                {
                    KoiReportId = Guid.NewGuid(),
                    KoiId = fish.KoiID,
                    CalculatedDate = DateTime.UtcNow,
                    Size = fishRequest.size,
                    Weight  = fishRequest.weight
                });
            


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

        public async Task<FishDetailResponse?> GetFishByIdAsync(Guid koiId, CancellationToken cancellationToken)
        {
            try
            {
                var fish = await _fishRepository.GetAsync(
                    predicate: f => f.KoiID == koiId,
                    include: query => query
                        .Include(f => f.Variety)
                        .Include(f => f.Pond)
                        .Include(f => f.RelKoiParameters), // Thêm bảng KoiReport
                    cancellationToken: cancellationToken
                );

                if (fish == null)
                    return null;

                return new FishDetailResponse
                {
                    FishId = fish.KoiID,
                    Name = fish.Name,
                    Image = fish.Image,
                    Price = fish.Price,
                    Sex = fish.Sex,
                    Physique = fish.Physique,
                    Breeder = fish.Breeder,
                    Age = fish.Age,
                    InPondSince = fish.InPondSince,
                    Variety = new VarietyInfo
                    {
                        VarietyId = fish.Variety.VarietyId,
                        VarietyName = fish.Variety.VarietyName,
                        Description = fish.Variety.Description
                    },
                    Pond = new PondInfo
                    {
                        PondID = fish.Pond.PondID,
                        Name = fish.Pond.Name,
                        CreateDate = fish.Pond.CreateDate,
                        Image = fish.Pond.Image,
                        MaxVolume = fish.Pond.MaxVolume
                    },
                    fishReportInfos = fish.RelKoiParameters.Select(r => new FishReportInfo
                    {
                        KoiReportId = r.KoiReportId,
                        KoiId = r.KoiId,
                        CalculatedDate = r.CalculatedDate,
                        Weight = r.Weight,
                        Size = r.Size
                    }).ToList(),
                    Notes = JsonSerializer.Deserialize<List<string>>(fish.Notes) ;

                };
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                return null;
            }
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
            existingFish.Physique = fishRequest.Physique;
            existingFish.Sex = fishRequest.Sex;
            existingFish.Breeder = fishRequest.Breeder;
            existingFish.Age = fishRequest.Age;
            existingFish.VarietyId = variety.VarietyId;
            existingFish.InPondSince = fishRequest.InPondSince;
            existingFish.Price = fishRequest.Price;

            existingFish.Image = fishRequest.Image;

            

             _relKoiparameterRepository.Insert(new KoiReport()
                {
                    KoiReportId = Guid.NewGuid(),
                    KoiId = existingFish.KoiID,
                    CalculatedDate = DateTime.UtcNow,
                    Weight = fishRequest.weight,
                    Size = fishRequest.size,
                });
            

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

        public async Task<List<FishDetailResponse>> GetAllFishAsync(string? name = null, CancellationToken cancellationToken = default)
        {
            var response = new List<FishDetailResponse>();

            try
            {
                var fishList = await _fishRepository.FindAsync(
                    predicate: !string.IsNullOrWhiteSpace(name)
                        ? f => f.Name.ToLower().Contains(name.ToLower())
                        : null,
                    include: query => query
                        .Include(f => f.Variety)
                        .Include(f => f.Pond),
                    orderBy: query => query.OrderBy(f => f.Name),
                    cancellationToken: cancellationToken
                );

                response = fishList.Select(f => new FishDetailResponse
                {
                    FishId = f.KoiID,
                    Name = f.Name,
                    Image = f.Image,
                    Price = f.Price,
                    Sex = f.Sex,
                    Physique = f.Physique,
                    Breeder = f.Breeder,
                    Age = f.Age,
                    InPondSince = f.InPondSince,
                    Variety = new VarietyInfo
                    {
                        VarietyId = f.Variety.VarietyId,
                        VarietyName = f.Variety.VarietyName,
                        Description = f.Variety.Description
                    }
                }).ToList();
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu cần, ví dụ log lỗi
            }

            return response;
        }


        public async Task<List<FishDto>> GetFishByOwnerId(Guid ownerId, CancellationToken cancellationToken = default)
        {
            var fishEntities = await _fishRepository.FindAsync(
                predicate: x => (x.Pond.OwnerId ?? "") == ownerId.ToString(),
                include: query => query
                    .Include(f => f.Variety)
                    .Include(f => f.Pond)
                    .Include(f => f.RelKoiParameters),
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
                Pond =  new PondDto
                {
                    PondID = f.Pond.PondID,
                    Name = f.Pond.Name,
                    OwnerId = f.Pond.OwnerId,
                    CreateDate = f.Pond.CreateDate,
                    Image = f.Pond.Image
                },
                Variety = new VarietyDto
                {
                    VarietyId = f.Variety.VarietyId,
                    VarietyName = f.Variety.VarietyName,
                    Description = f.Variety.Description,
                    AuthorId = f.Variety.AuthorId
                },
                fishReportInfos = f.RelKoiParameters.Select(r => new FishReportInfo
                {
                    KoiReportId = r.KoiReportId,
                    KoiId = r.KoiId,
                    CalculatedDate = r.CalculatedDate,
                    Weight = r.Weight,
                    Size = r.Size
                }).ToList()
            }).ToList();

            return fishDtos;
        }

        public async Task<bool> AddNote(Guid koiId, string note)
        {
            var fishes = await _fishRepository.FindAsync(u => u.KoiID == koiId);
            var fish = fishes.FirstOrDefault();
            if (fish == null) {
                return false;
            }

            var notes = JsonSerializer.Deserialize<List<string>>(fish.Notes);
            notes.Add(note);
            fish.Notes = JsonSerializer.Serialize(notes);
            _fishRepository.Update(fish);
            await _unitOfWork.SaveChangesAsync();


            return true;
        }
    }
}