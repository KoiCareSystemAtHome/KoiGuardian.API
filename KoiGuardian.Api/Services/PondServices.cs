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

        Task<PondResponse> UpdateIOTPond(UpdatePondIOTRequest Request, CancellationToken cancellation);


    }

    public class PondServices(
        IRepository<Pond> pondRepository,
        IRepository<PondStandardParam> pondStandardparameterRepository,
        IRepository<RelPondParameter> relPondparameterRepository,
        IRepository<PondStandardParam> parameterRepository,
        IRepository<Product> productRepository,
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
                    MaxVolume = request.MaxVolume,

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
                        CalculatedDate = DateTime.UtcNow.AddHours(7),
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
                pond.MaxVolume = request.MaxVolume; 

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
                        CalculatedDate = DateTime.UtcNow.AddHours(7),
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
                MaxVolume = p.MaxVolume,    
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
                    response = new PondDetailResponse
                    {
                        PondID = pond.PondID,
                        Name = pond.Name,
                        Image = pond.Image,
                        CreateDate = pond.CreateDate,
                        OwnerId = pond.OwnerId,
                        MaxVolume = pond.MaxVolume,
                        PondParameters = pond.RelPondParameter
                            .GroupBy(rp => rp.ParameterID)
                            .Select(group => new PondParameterInfo
                            {
                                ParameterUnitID = group.Key,
                                UnitName = group.First().Parameter.UnitName,
                                ParameterName = group.First().Parameter.Name,
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

                    var latestParameters = pond.RelPondParameter
                        .GroupBy(rp => rp.ParameterID)
                        .Select(g => g.OrderByDescending(rp => rp.CalculatedDate).First())
                        .ToList();

                    response.Recomment = await GetProductRecommendations(latestParameters);
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu cần
            }

            return response;
        }

        private async Task<List<ProductRecommentInfo>> GetProductRecommendations(List<RelPondParameter> latestParameters)
        {
            // Gộp thông số cần điều chỉnh
            var adjustmentsNeeded = latestParameters
                .Where(p => (p.Parameter.DangerLower.HasValue && p.Value < p.Parameter.DangerLower.Value) ||
                            (p.Parameter.DangerUpper.HasValue && p.Value > p.Parameter.DangerUpper.Value) ||
                            (p.Parameter.WarningLowwer.HasValue && p.Value < p.Parameter.WarningLowwer.Value) ||
                            (p.Parameter.WarningUpper.HasValue && p.Value > p.Parameter.WarningUpper.Value))
                .ToDictionary(
                    p => p.Parameter.Name,
                    p => (p.Parameter.DangerLower.HasValue && p.Value < p.Parameter.DangerLower.Value) ||
                         (p.Parameter.WarningLowwer.HasValue && p.Value < p.Parameter.WarningLowwer.Value)
                        ? "Increased" : "Decreased"
                );

            if (!adjustmentsNeeded.Any())
                return new List<ProductRecommentInfo>();

            // Truy vấn với FindAsync
            var patterns = adjustmentsNeeded.Select(a => $"\"{a.Key}\":\"{a.Value}\"");
            var products = await productRepository.FindAsync(
                p => patterns.Any(pattern => p.ParameterImpactment.Contains(pattern)),
                CancellationToken.None
            );

            if (products == null || !products.Any())
                return new List<ProductRecommentInfo>();

            // Trả ra ProductId, xếp hạng theo Score
            return products
                .Select(p => new
                {
                    ProductId = p.ProductId,
                })
                .Select(x => new
                {
                    ProductId = x.ProductId,

                })
                .Select(x => new ProductRecommentInfo { Productid = x.ProductId })
                .ToList();
        }


        public async Task<List<PondDto>> GetAllPondByOwnerId(Guid ownerId, CancellationToken cancellationToken = default)
        {
            const int DaysSinceLastUpdateThreshold = 14;

            // Lấy danh sách ao thuộc về chủ sở hữu
            var pondEntities = await pondRepository.FindAsync(
                predicate: pond => pond.OwnerId.Equals(ownerId.ToString()),
                include: query => query.Include(p => p.Fish).ThenInclude(f => f.Variety),
                orderBy: query => query.OrderBy(p => p.Name),
                cancellationToken: cancellationToken
            );
            var ponds = pondEntities.ToList();

            // Tạo DTOs
            var pondDtos = new List<PondDto>();

            foreach (var pond in ponds)
            {
                // Load lần đo gần nhất của các parameter cho hồ
                var pondParameters = await relPondparameterRepository.FindAsync(
                    rp => rp.PondId == pond.PondID,
                    orderBy: query => query.OrderByDescending(rp => rp.CalculatedDate),
                    cancellationToken: cancellationToken
                );
                var latestParameters = pondParameters
                    .GroupBy(rp => rp.ParameterID)
                    .Select(g => g.First()) // Lấy lần đo gần nhất cho mỗi parameter
                    .ToList();

                string status = "Normal";
                string statusDescription = "";

                if (latestParameters.Any())
                {
                    // Load master data parameters
                    var parameterIds = latestParameters.Select(rp => rp.ParameterID).ToList();
                    var parameters = await parameterRepository.FindAsync( // Sử dụng _parameterRepository
                        p => parameterIds.Contains(p.ParameterID),
                        cancellationToken: cancellationToken
                    );
                    var parameterLookup = parameters.ToDictionary(p => p.ParameterID);

                    // Kiểm tra lần cập nhật cuối cùng
                    var latestUpdate = latestParameters.Max(rp => rp.CalculatedDate);
                    var daysSinceLastUpdate = (DateTime.UtcNow.AddHours(7) - latestUpdate).Days;

                    if (daysSinceLastUpdate > DaysSinceLastUpdateThreshold)
                    {
                        status = "Warning";
                        statusDescription = $"Quá lâu chưa cập nhật hồ (last update: {daysSinceLastUpdate} days ago)";
                    }
                    else
                    {
                        // Kiểm tra lần đo gần nhất của từng parameter
                        foreach (var param in latestParameters)
                        {
                            if (!parameterLookup.TryGetValue(param.ParameterID, out var parameter) || parameter.DangerUpper == null)
                                continue;

                            var currentValue = param.Value;
                            var maxSafeDensity = (double)parameter.DangerUpper;
                            var warningUpper = parameter.WarningUpper;

                            if (currentValue >= maxSafeDensity)
                            {
                                status = "Danger";
                                statusDescription = $"{parameter.Name} chạm ngưỡng nguy hiểm ({currentValue}/{maxSafeDensity})";
                                break; // Nguy hiểm thì dừng luôn
                            }
                            else if (warningUpper.HasValue && currentValue > warningUpper.Value)
                            {
                                if (status != "Danger") // Chỉ cập nhật nếu chưa là Danger
                                {
                                    status = "Warning";
                                    statusDescription = $"{parameter.Name} vượt ngưỡng cảnh báo ({currentValue}/{warningUpper.Value})";
                                }
                            }
                        }
                    }
                }
                else
                {
                    status = "Warning";
                    statusDescription = "No data available for the pond";
                }

                // Tạo DTO cho hồ
                var pondDto = new PondDto
                {
                    PondID = pond.PondID,
                    Name = pond.Name,
                    OwnerId = pond.OwnerId,
                    CreateDate = pond.CreateDate,
                    Image = pond.Image,
                    MaxVolume = pond.MaxVolume,
                    Status = status, // Trạng thái
                    StatusDescription = statusDescription, // Mô tả chi tiết
                    FishAmount = pond.Fish.Count(),
                    /*Fish = pond.Fish.Select(f => new FishDto
                    {
                        KoiID = f.KoiID,
                        Name = f.Name,
                        Image = f.Image,
                        Price = f.Price,
                        Sex = f.Sex,
                        Age = f.Age,
                    }).ToList()*/
                };

                pondDtos.Add(pondDto);
            }

            return pondDtos;
        }

        public async Task<PondResponse> UpdateIOTPond(UpdatePondIOTRequest Request, CancellationToken cancellation)
        {
            var requirementsParam = await RequireParam(cancellation);
            var response = new PondResponse();
            var pond = await pondRepository.GetAsync(x => x.PondID.Equals(Request.PondID), cancellation);
            if (pond != null)
            {

                var validValues = Request.RequirementPondParam.Where(u =>
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
                        CalculatedDate = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7), DateTimeKind.Utc),
                        Value = validValue.Value
                    });
                }
                try
                {
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
    }
}
