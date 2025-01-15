using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.DataAccess;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Azure.Core;
using Azure;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.Api.Services
{
    public interface IPondServices
    {
        Task<PondResponse> CreatePond(CreatePondRequest Request, CancellationToken cancellation);
        Task<PondResponse> UpdatePond(UpdatePondRequest Request, CancellationToken cancellation);
        Task<List<PondRerquireParam>> RequireParam(CancellationToken cancellation);

        Task<List<Pond>> GetAllPondhAsync(string? name = null, CancellationToken cancellationToken = default);
        Task<PondDetailResponse> GetPondById(Guid pondId, CancellationToken cancellation);


    }

    public class PondServices(
        IRepository<Pond> pondRepository,
        IRepository<Parameter> parameterRepository,
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
                    requirementsParam.Select(u => u.HistoryId).Contains(u.HistoryId)
                    );

                foreach (var validValue in validValues)
                {
                    relPondparameterRepository.Insert(new RelPondParameter()
                    {
                        RelPondParameterId = Guid.NewGuid(),
                        PondId = pond.PondID,
                        ParameterHistoryId = validValue.HistoryId,
                        CalculatedDate = DateTime.UtcNow,
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
            return (await parameterRepository.FindAsync(
                u => u.Type == ParameterType.Pond.ToString()
                    && u.IsActive && u.ValidUntil == null,
                cancellationToken: cancellation))
                .Select(u => new PondRerquireParam()
                {
                    HistoryId = u.ParameterID,
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
                    requirementsParam.Select(u => u.HistoryId).Contains(u.HistoryId)
                    );

                foreach (var validValue in validValues)
                {
                    relPondparameterRepository.Insert(new RelPondParameter()
                    {
                        RelPondParameterId = Guid.NewGuid(),
                        PondId = pond.PondID,
                        ParameterHistoryId = validValue.HistoryId,
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

        public async Task<List<Pond>> GetAllPondhAsync(string? name = null, CancellationToken cancellationToken = default)
        {
            // Gọi FindAsync với predicate và các tham số khác
            var result = await pondRepository.FindAsync(
                predicate: !string.IsNullOrWhiteSpace(name)
                    ? f => f.Name.ToLower().Contains(name.ToLower())
                    : null, // Không lọc nếu name là null hoặc rỗng
                include: query => query
                    .Include(p => p.RelPondParameter)
                            .ThenInclude(pu => pu.Parameter)
                    .Include(p => p.Fish)
                    .Include(p => p.Mode), // Thêm quan hệ liên quan nếu cần
                orderBy: query => query.OrderBy(f => f.Name), // Sắp xếp theo Name
                cancellationToken: cancellationToken
            );

            // Chuyển đổi kết quả thành danh sách
            return result.ToList();
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
                                .ThenInclude(pu => pu.Parameter)
                        .Include(p => p.Fish)
                        .Include(p => p.Mode),
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
                        PondParameters = pond.RelPondParameter.Select(rp => new PondParameterInfo
                        {
                            ParameterUnitID = rp.Parameter.HistoryId,
                            UnitName = rp.Parameter.UnitName,
                            WarningLowwer = rp.Parameter.WarningLowwer,
                            WarningUpper = rp.Parameter.WarningUpper,
                            DangerLower = rp.Parameter.DangerLower,
                            DangerUpper = rp.Parameter.DangerUpper,
                            MeasurementInstruction = rp.Parameter.MeasurementInstruction
                        }).ToList(),
                        Fish = pond.Fish.Select(f => new FishInfo
                        {
                            FishId = f.KoiID,
                            FishName = f.Name
                        }).ToList(),
                        FeedingMode = pond.Mode != null ? new FeedingModeInfo
                        {
                            FeedingModeId = pond.Mode.ModeId,
                            ModeName = pond.Mode.ModeName
                        } : null
                    };
                }
                else
                {

                }
            }
            catch (Exception ex)
            {

            }

            return response;
        }



    }
}
