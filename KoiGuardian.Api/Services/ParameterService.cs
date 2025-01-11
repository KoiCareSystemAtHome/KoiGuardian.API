using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.DataAccess;
using OfficeOpenXml;
using KoiGuardian.Models.Response;

namespace KoiGuardian.Api.Services
{
    public interface IParameterService
    {
        Task<ParameterResponse> UpsertFromExcel(IFormFile file, CancellationToken cancellationToken);
        Task<Parameter> getAll(Guid parameterId, CancellationToken cancellationToken);
        Task<List<Parameter>> getAll(string parameterType, CancellationToken cancellationToken);
    }

    public class ParameterService : IParameterService
    {
        private readonly IRepository<Parameter> _parameterRepository;
        private readonly IRepository<ParameterUnit> _parameterUnitRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public ParameterService(
            IRepository<Parameter> parameterRepository,
            IRepository<ParameterUnit> parameterUnitRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork)
        {
            _parameterRepository = parameterRepository;
            _parameterUnitRepository = parameterUnitRepository;
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

                var parameters = new List<Parameter>();
                var parameterUnits = new List<ParameterUnit>();

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

                    var unitIdString = worksheet.Cells[row, 4].GetValue<string>();
                    if (!Guid.TryParse(unitIdString, out var unitId))
                    {
                        response.status = "400";
                        response.message = $"Invalid Guid format in row {row}, column 4.";
                        return response;
                    }

                    var unitName = worksheet.Cells[row, 5].GetValue<string>();

                    // Ensure that Warning and Danger values are handled safely
                    var warningUpper = worksheet.Cells[row, 6].GetValue<double?>();
                    var warningLower = worksheet.Cells[row, 7].GetValue<double?>();
                    var dangerUpper = worksheet.Cells[row, 8].GetValue<double?>();
                    var dangerLower = worksheet.Cells[row, 9].GetValue<double?>();
                    var isStandard = worksheet.Cells[row, 10].GetValue<bool>(); // Assuming it's in column 10
                    var isActive = worksheet.Cells[row, 11].GetValue<bool>();  // Assuming it's in column 11
                    var conversionRate = worksheet.Cells[row, 12].GetValue<float>(); // Assuming it's in column 12
                    var measurementInstruction = worksheet.Cells[row, 13].GetValue<string>(); // Assuming it's in column 13
                    var ageFrom = worksheet.Cells[row, 14].GetValue<int>();  // Assuming it's in column 14
                    var ageTo = worksheet.Cells[row, 15].GetValue<int>();    // Assuming it's in column 15

                    // Upsert Parameter
                    var parameter = await _parameterRepository.GetAsync(p => p.ParameterID == parameterId, cancellationToken);
                    if (parameter == null)
                    {
                        parameter = new Parameter
                        {
                            ParameterID = parameterId,
                            Name = parameterName,
                            Type = parameterType.ToString(),
                            CreatedAt = DateTime.UtcNow
                        };
                        _parameterRepository.Insert(parameter);
                    }
                    else
                    {
                        parameter.Name = parameterName;
                        parameter.Type = parameterType.ToString();
                        _parameterRepository.Update(parameter);
                    }

                    // Upsert Parameter Unit
                    var parameterUnit = await _parameterUnitRepository.GetAsync(pu => pu.ParameterUnitID == unitId, cancellationToken);
                    if (parameterUnit == null)
                    {
                        parameterUnit = new ParameterUnit
                        {
                            ParameterUnitID = unitId,
                            ParameterID = parameterId,
                            UnitName = unitName,
                            WarningUpper = warningUpper,
                            WarningLowwer = warningLower,
                            DangerUpper = dangerUpper,
                            DangerLower = dangerLower,
                            IsStandard = isStandard,
                            IsActive = isActive,
                            Convertionrate = conversionRate,
                            MeasurementInstruction = measurementInstruction,
                            AgeFrom = ageFrom,
                            AgeTo = ageTo
                        };
                        _parameterUnitRepository.Insert(parameterUnit);
                    }
                    else
                    {
                        parameterUnit.UnitName = unitName;
                        parameterUnit.WarningUpper = warningUpper;
                        parameterUnit.WarningLowwer = warningLower;
                        parameterUnit.DangerUpper = dangerUpper;
                        parameterUnit.DangerLower = dangerLower;
                        parameterUnit.IsStandard = isStandard;
                        parameterUnit.IsActive = isActive;
                        parameterUnit.Convertionrate = conversionRate;
                        parameterUnit.MeasurementInstruction = measurementInstruction;
                        parameterUnit.AgeFrom = ageFrom;
                        parameterUnit.AgeTo = ageTo;
                        _parameterUnitRepository.Update(parameterUnit);
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

        public async Task<List<Parameter>> getAll(string parameterType, CancellationToken cancellationToken)
        {
            return (await _parameterRepository.FindAsync(u => u.Type == parameterType)).ToList();
        }
    }
}
