using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.DataAccess;
using OfficeOpenXml;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;
using KoiGuardian.Models.Request;

namespace KoiGuardian.Api.Services
{
    public interface IParameterService
    {
        Task<ParameterResponse> UpsertFromExcel(IFormFile file, CancellationToken cancellationToken);
        Task<KoiStandardParam> getAll(Guid parameterId, CancellationToken cancellationToken);
        Task<List<object>> getAll(string parameterType,CancellationToken cancellationToken);
    }

    public class ParameterService : IParameterService
    {
        private readonly IRepository<KoiStandardParam> _parameterRepository;
        private readonly IRepository<PondStandardParam> _pondParameterRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public ParameterService(
            IRepository<KoiStandardParam> parameterRepository,
            IRepository<PondStandardParam> pparameterRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork)
        {
            _parameterRepository = parameterRepository;
            _unitOfWork = unitOfWork;
            _pondParameterRepository = pparameterRepository;
        }

        public async Task<ParameterResponse> UpsertFromExcel(IFormFile file, CancellationToken cancellationToken)
        {
            var response = new ParameterResponse();
            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream, cancellationToken);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    response.status = "400";
                    response.message = "No worksheet found in the Excel file.";
                    return response;
                }

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var parameterIdString = worksheet.Cells[row, 1].GetValue<string>();
                    if (!Guid.TryParse(parameterIdString, out var parameterId))
                    {
                        response.status = "400";
                        response.message = $"Invalid Guid format in row {row}, column 1.";
                        return response;
                    }

                    var parameterName = worksheet.Cells[row, 2].GetValue<string>();
                    var parameterTypeString = worksheet.Cells[row, 3].GetValue<string>();
                    if (!Enum.TryParse(parameterTypeString, out ParameterType parameterType))
                    {
                        response.status = "400";
                        response.message = $"Invalid ParameterType format in row {row}, column 3.";
                        return response;
                    }  
                        
                        var isActive = worksheet.Cells[row, 4].GetValue<bool>();
                        var measurementInstruction = worksheet.Cells[row, 5].GetValue<string>();
                        var age = worksheet.Cells[row, 6].GetValue<int>();
                        var unitName = worksheet.Cells[row, 7].GetValue<string>();
                        var warningUpper = worksheet.Cells[row, 8].GetValue<double?>();
                        var warningLower = worksheet.Cells[row, 9].GetValue<double?>();
                        var dangerUpper = worksheet.Cells[row, 10].GetValue<double?>();
                        var dangerLower = worksheet.Cells[row, 11].GetValue<double?>();
                        var validUntil = worksheet.Cells[row, 12].GetValue<DateTime?>();

                    if (parameterType == ParameterType.Fish)
                    {
                       

                        var parameter = await _parameterRepository.GetAsync(p => p.ParameterID == parameterId, cancellationToken);
                        if (parameter == null)
                        {
                            parameter = new KoiStandardParam
                            {
                                ParameterID = parameterId,
                                CreatedAt = DateTime.UtcNow,
                                IsActive = isActive,
                                MeasurementInstruction = measurementInstruction,
                                Age = age
                            };
                            _parameterRepository.Insert(parameter);
                        }
                        else
                        {
                            parameter.IsActive = isActive;
                            parameter.MeasurementInstruction = measurementInstruction;
                            parameter.Age = age;
                            _parameterRepository.Update(parameter);
                        }
                    }
                    else if (parameterType == ParameterType.Pond)
                    {
                        var pondParameter = await _pondParameterRepository.GetAsync(p => p.ParameterID == parameterId, cancellationToken);
                        if (pondParameter == null)
                        {
                            pondParameter = new PondStandardParam
                            {
                                ParameterID = parameterId,
                                Name = parameterName,
                                Type = parameterType.ToString(),
                                UnitName = unitName,
                                WarningUpper = warningUpper,
                                WarningLowwer = warningLower,
                                DangerUpper = dangerUpper,
                                DangerLower = dangerLower,
                                IsActive = isActive,
                                MeasurementInstruction = measurementInstruction,
                                //ValidUntil = validUntil,
                                CreatedAt = DateTime.UtcNow
                            };
                            _pondParameterRepository.Insert(pondParameter);
                        }
                        else
                        {
                            pondParameter.Name = parameterName;
                            pondParameter.UnitName = unitName;
                            pondParameter.WarningUpper = warningUpper;
                            pondParameter.WarningLowwer = warningLower;
                            pondParameter.DangerUpper = dangerUpper;
                            pondParameter.DangerLower = dangerLower;
                            pondParameter.IsActive = isActive;
                            pondParameter.MeasurementInstruction = measurementInstruction;
                            pondParameter.ValidUntil = validUntil;
                            _pondParameterRepository.Update(pondParameter);
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                response.status = "200";
                response.message = "Upsert operation completed successfully.";
            }
            catch (Exception ex)
            {
                response.status = "500";
                response.message = $"An error occurred: {ex.Message}";
            }

            return response;
        }



        public async Task<KoiStandardParam> getAll(Guid parameterId, CancellationToken cancellationToken)
        {
            return await _parameterRepository.GetAsync(p => p.ParameterID == parameterId, cancellationToken);

        }

        public async Task<List<object>> getAll(string parameterType, CancellationToken cancellationToken)
        {
            if (parameterType.ToLower() == "pond")
            {
                return (await _pondParameterRepository.FindAsync(
                    u => u.IsActive
                        ,
                    cancellationToken: cancellationToken))
                    .Select(u => new PondRerquireParam()
                    {
                        ParameterName = u.Name,
                        UnitName = u.UnitName,
                        WarningLowwer = u.WarningLowwer,
                        WarningUpper = u.WarningUpper,
                        DangerLower = u.DangerLower,
                        DangerUpper = u.DangerUpper,
                        MeasurementInstruction = u.MeasurementInstruction,
                        ParameterId = u.ParameterID
                    }).ToList<object>();
            }else
            {
                return (await _parameterRepository.FindAsync(
                    u => u.IsActive,
                    cancellationToken: cancellationToken)).ToList<object>();
            }


            
        }
    }
}
