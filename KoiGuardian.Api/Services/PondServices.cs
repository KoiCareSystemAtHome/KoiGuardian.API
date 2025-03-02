using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.DataAccess;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Azure.Core;
using Azure;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KoiGuardian.Api.Services
{
    public interface IPondServices
    {
        Task<PondResponse> CreatePond(CreatePondRequest Request, CancellationToken cancellation);
        Task<PondResponse> UpdatePond(UpdatePondRequest Request, CancellationToken cancellation);
        Task<List<PondRerquireParam>> RequireParam(CancellationToken cancellation);

        Task<List<PondDto>> GetAllPondhAsync(string? name = null, CancellationToken cancellationToken = default);
        Task<PondDetailResponse> GetPondById(Guid pondId, CancellationToken cancellation);
        Task<List<PondDto>> GetAllPondByOwnerId(Guid ownerId, CancellationToken cancellationToken = default);


    }

    public class PondServices(
        IRepository<Pond> pondRepository,
        IRepository<PondStandardParam> pondStandardparameterRepository,
        IRepository<RelPondParameter> relPondparameterRepository,
        KoiGuardianDbContext _dbContext,
        IImageUploadService imageUpload,
        IRepository<User> userRepository) : IPondServices
    {
        public async Task<PondResponse> CreatePond(CreatePondRequest request, CancellationToken cancellation)
        {
            var requirementsParam = await RequireParam(cancellation);

            var response = new PondResponse();
            try
            {
                var pond = new Pond
                {
                    PondID = Guid.NewGuid(),
                    OwnerId = request.OwnerId,
                    CreateDate = request.CreateDate,
                    Name = request.Name,
                    Image = request.Image,

                };


                pondRepository.Insert(pond);

                var validValues = request.RequirementPondParam.Where(u =>
                    requirementsParam.Select(u => u.ParameterId).Contains(u.HistoryId)
                    );

                foreach (var validValue in validValues)
                {
                    relPondparameterRepository.Insert(new RelPondParameter()
                    {
                        RelPondParameterId = Guid.NewGuid(),
                        PondId = pond.PondID,
                        ParameterHistoryId = validValue.HistoryId,
                        CalculatedDate = DateTime.UtcNow,
                        ParameterID = validValue.HistoryId,
                        Value = validValue.Value
                    });
                }


                await _dbContext.SaveChangesAsync(cancellation);

                response.status = "201";
                response.message = "Pond created successfully";
            }
            catch (Exception ex)
            {
                response.status = "500";
                response.message = $"An error occurred: {ex.Message}";
            }

            return response;
        }

        public async Task<List<PondRerquireParam>> RequireParam(CancellationToken cancellation)
        {
            return (await pondStandardparameterRepository.FindAsync(
                u => u.Type.ToLower() == ParameterType.Pond.ToString().ToLower()
                    && u.IsActive && u.ValidUntil == null,
                cancellationToken: cancellation))
                .Select(u => new PondRerquireParam()
                {
                    ParameterId = u.ParameterID,
                    ParameterName = u.Name,
                    UnitName = u.UnitName,
                    WarningLowwer = u.WarningLowwer,
                    WarningUpper = u.WarningUpper,
                    DangerLower = u.DangerLower,
                    DangerUpper = u.DangerUpper,
                    MeasurementInstruction = u.MeasurementInstruction,
                }).ToList();
        }

        public async Task<PondResponse> UpdatePond(UpdatePondRequest request, CancellationToken cancellation)
        {
            var requirementsParam = await RequireParam(cancellation);
            var response = new PondResponse();
            var pond = await pondRepository.GetAsync(x => x.PondID.Equals(request.PondID), cancellation);
            if (pond != null)
            {
                pond.OwnerId = request.OwnerId;
                pond.CreateDate = request.CreateDate;
                pond.Name = request.Name;
                pond.Image = request.Image;

                var validValues = request.RequirementPondParam.Where(u =>
                    requirementsParam.Select(u => u.ParameterId).Contains(u.HistoryId)
                    );

                foreach (var validValue in validValues)
                {
                    relPondparameterRepository.Insert(new RelPondParameter()
                    {
                        RelPondParameterId = Guid.NewGuid(),
                        PondId = pond.PondID,
                        ParameterHistoryId = validValue.HistoryId,
                        ParameterID = validValue.HistoryId, 
                        CalculatedDate = DateTime.UtcNow,
                        Value = validValue.Value
                    });
                }
                try
                {
                    pondRepository.Update(pond);
                    await _dbContext.SaveChangesAsync(cancellation);

                    response.status = "201";
                    response.message = "Update Ponnd Success";
                }
                catch (Exception ex)
                {
                    response.status = "500";
                    response.message = $"An error occurred: {ex.Message}";
                }
            }
            else
            {
                response.status = "409 ";
                response.message = "Pond Haven't Existed";
            }
            return response;
        }
        public async Task<List<PondDto>> GetAllPondhAsync(string? name = null, CancellationToken cancellationToken = default)
        {
            // Nếu name có giá trị, thực hiện tìm kiếm, nếu không thì lấy tất cả
            Expression<Func<Pond, bool>> predicate = p => true;
            if (!string.IsNullOrWhiteSpace(name))
            {
                string lowerName = name.ToLower();
                predicate = f => f.Name.ToLower().Contains(lowerName);
            }

            // Gọi FindAsync với predicate và các tham số khác
            var pondEntities = await pondRepository.FindAsync(
                predicate: predicate,
                include: query => query
                    .Include(p => p.RelPondParameter)
                        .ThenInclude(pu => pu.Parameter)
                    .Include(p => p.Fish)
                        .ThenInclude(f => f.Variety), // Load thông tin Variety của Fish
                orderBy: query => query.OrderBy(f => f.Name), // Sắp xếp theo Name
                cancellationToken: cancellationToken
            );

            // Chuyển đổi kết quả thành danh sách DTO
            var pondDtos = pondEntities.Select(p => new PondDto
            {
                PondID = p.PondID,
                Name = p.Name,
                OwnerId = p.OwnerId,
                CreateDate = p.CreateDate,
                Image = p.Image,
            }).ToList();

            return pondDtos;
        }






        public async Task<PondDetailResponse> GetPondById(Guid pondId, CancellationToken cancellation)
        {
            var response = new PondDetailResponse();
            try
            {
                var pond = await pondRepository.GetAsync(
                    predicate: p => p.PondID == pondId,
                    include: query => query
                        .Include(p => p.RelPondParameter)
                            .ThenInclude(rp => rp.Parameter)
                        .Include(p => p.Fish),
                    cancellationToken: cancellation
                );

                if (pond != null)
                {
                    return new PondDetailResponse
                    {
                        PondID = pond.PondID,
                        Name = pond.Name,
                        Image = pond.Image,
                        CreateDate = pond.CreateDate,
                        OwnerId = pond.OwnerId,
                        PondParameters = pond.RelPondParameter
                            .GroupBy(rp => rp.ParameterID) // Nhóm theo ParameterID
                            .Select(group => new PondParameterInfo
                            {
                                ParameterUnitID = group.Key, // Lấy ParameterID
                                UnitName = group.First().Parameter.UnitName,
                                WarningLowwer = group.First().Parameter.WarningLowwer,
                                WarningUpper = group.First().Parameter.WarningUpper,
                                DangerLower = group.First().Parameter.DangerLower,
                                DangerUpper = group.First().Parameter.DangerUpper,
                                MeasurementInstruction = group.First().Parameter.MeasurementInstruction,
                                valueInfors = group.Select(rp => new ValueInfor
                                {
                                    caculateDay = rp.CalculatedDate,
                                    Value = rp.Value
                                }).ToList()
                            }).ToList(),
                        Fish = pond.Fish.Select(f => new FishInfo
                        {
                            FishId = f.KoiID,
                            FishName = f.Name
                        }).ToList()
                    };
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu cần
            }

            return response;
        }


        public async Task<List<PondDto>> GetAllPondByOwnerId(Guid ownerId, CancellationToken cancellationToken = default)
        {
            // Lấy danh sách ao thuộc về chủ sở hữu
            var pondEntities = await pondRepository.FindAsync(
                predicate: pond => pond.OwnerId.Equals(ownerId.ToString()),
                include: query => query.Include(p => p.Fish).ThenInclude(f => f.Variety),
                orderBy: query => query.OrderBy(p => p.Name),
                cancellationToken: cancellationToken
            );
            pondEntities.ToList();

            // Ánh xạ sang DTO
            var pondDtos = pondEntities.Select(p => new PondDto
            {
                PondID = p.PondID,
                Name = p.Name,
                OwnerId = p.OwnerId,
                CreateDate = p.CreateDate,
                Image = p.Image,
                Fish = p.Fish.Select(f => new FishDto
                {
                    KoiID = f.KoiID,
                    Name = f.Name,
                    Image = f.Image,
                    Price = f.Price,
                    Sex = f.Sex,
                    Age = f.Age,
                }).ToList()
            }).ToList();

            return pondDtos;
        }


    }
}
