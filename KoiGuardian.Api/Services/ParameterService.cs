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
        Task<Parameter> getAll(Guid parameterId, CancellationToken cancellationToken);
        Task<List<PondRerquireParam>> getAll(string parameterType, int age, CancellationToken cancellationToken);
    }

    public class ParameterService : IParameterService
    {
        private readonly IRepository<Parameter> _parameterRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public ParameterService(
            IRepository<Parameter> parameterRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork)
        {
            _parameterRepository = parameterRepository;
            _unitOfWork = unitOfWork;
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

                    var unitName = worksheet.Cells[row, 4].GetValue<string>();

                    // Ensure that Warning and Danger values are handled safely
                    var warningUpper = worksheet.Cells[row, 5].GetValue<double?>();
                    var warningLower = worksheet.Cells[row, 6].GetValue<double?>();
                    var dangerUpper = worksheet.Cells[row, 7].GetValue<double?>();
                    var dangerLower = worksheet.Cells[row, 8].GetValue<double?>();
                    var isActive = worksheet.Cells[row, 9].GetValue<bool>(); // Assuming it's in column 9
                    var measurementInstruction = worksheet.Cells[row, 10].GetValue<string>(); // Assuming it's in column 10
                    var ageFrom = worksheet.Cells[row, 11].GetValue<int>();  // Assuming it's in column 11
                    var ageTo = worksheet.Cells[row, 12].GetValue<int>();    // Assuming it's in column 12

                    // Upsert Parameter
                    var parameter = await _parameterRepository.GetAsync(p => p.ParameterID == parameterId, cancellationToken);
                    if (parameter == null)
                    {
                        parameter = new Parameter
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
                            AgeFrom = ageFrom,
                            AgeTo = ageTo,
                            CreatedAt = DateTime.UtcNow
                        };
                        _parameterRepository.Insert(parameter);
                    }
                    else
                    {
                        parameter.Name = parameterName;
                        parameter.Type = parameterType.ToString();
                        parameter.UnitName = unitName;
                        parameter.WarningUpper = warningUpper;
                        parameter.WarningLowwer = warningLower;
                        parameter.DangerUpper = dangerUpper;
                        parameter.DangerLower = dangerLower;
                        parameter.IsActive = isActive;
                        parameter.MeasurementInstruction = measurementInstruction;
                        parameter.AgeFrom = ageFrom;
                        parameter.AgeTo = ageTo;
                        _parameterRepository.Update(parameter);
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


        public async Task<Parameter> getAll(Guid parameterId, CancellationToken cancellationToken)
        {
            return await _parameterRepository.GetAsync(p => p.ParameterID == parameterId, cancellationToken);

        }

        public async Task<List<PondRerquireParam>> getAll(string parameterType, int age, CancellationToken cancellationToken)
        {
            if (age < 1)
            {
                return (await _parameterRepository.FindAsync(
                    u => u.Type.ToLower() == parameterType.ToLower()
                        && u.IsActive && u.ValidUntil == null,
                    cancellationToken: cancellationToken))
                    .Select(u => new PondRerquireParam()
                    {
                        HistoryId = u.HistoryId,
                        ParameterName = u.Name,
                        UnitName = u.UnitName,
                        WarningLowwer = u.WarningLowwer,
                        WarningUpper = u.WarningUpper,
                        DangerLower = u.DangerLower,
                        DangerUpper = u.DangerUpper,
                        MeasurementInstruction = u.MeasurementInstruction,

                    }).ToList();
            }

            return (await _parameterRepository.FindAsync(
                    u => u.Type.ToLower() == parameterType.ToLower()
                        && u.IsActive && u.ValidUntil == null
                        && u.AgeFrom <= age && u.AgeTo >= age
                        ,
                    cancellationToken: cancellationToken))
                    .Select(u => new PondRerquireParam()
                    {
                        HistoryId = u.HistoryId,
                        ParameterName = u.Name,
                        UnitName = u.UnitName,
                        WarningLowwer = u.WarningLowwer,
                        WarningUpper = u.WarningUpper,
                        DangerLower = u.DangerLower,
                        DangerUpper = u.DangerUpper,
                        MeasurementInstruction = u.MeasurementInstruction,

                    }).ToList();
        }
    }
}
