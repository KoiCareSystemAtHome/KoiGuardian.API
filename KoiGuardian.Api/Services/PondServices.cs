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
        Task<PondResponse> CreatePond(string baseUrl, CreatePondRequest Request, CancellationToken cancellation);
        Task<PondResponse> UpdatePond(UpdatePondRequest Request, CancellationToken cancellation);
        Task<List<PondRerquireParam>> RequireParam(CancellationToken cancellation);
    }

    public class PondServices(
        IRepository<Pond> pondRepository, 
        IRepository<ParameterUnit> parameterUnitRepository,
        IRepository<RelPondParameter> relPondparameterRepository,
        KoiGuardianDbContext _dbContext, 
        IImageUploadService imageUpload,
        IRepository<User> userRepository) : IPondServices
    {
        public async Task<PondResponse> CreatePond(string baseUrl, CreatePondRequest request, CancellationToken cancellation)
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
                    Name = request.Name
                };
                var image = await imageUpload.UploadImageAsync(baseUrl, "Pond", pond.PondID.ToString(),request.Image);
                pond.Image = image;

                var validValues = request.RequirementPondParam.Where ( u=>
                    requirementsParam.SelectMany(u => u.ParameterUnits?.Select( u => u.ParameterUntiID)).Contains(u.ParamterUnitID)
                    );

                foreach ( var validValue in validValues)
                {
                    relPondparameterRepository.Insert(new RelPondParameter()
                    {
                        RelPondParameterId = Guid.NewGuid(),
                        PondId = pond.PondID,
                        ParameterUnitID = validValue.ParamterUnitID,
                        CalculatedDate = DateTime.Now,
                        Value = validValue.Value
                    });
                }

                pondRepository.Insert(pond);
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

        public async Task<List<PondRerquireParam>> RequireParam( CancellationToken cancellation)
        {
            return (await parameterUnitRepository.FindAsync(
                u => u.Parameter.Type == ParameterType.Pond.ToString()
                    &&  u.IsActive && u.IsStandard && u.ValidUnitl == null,
                u => u.Include(p => p.Parameter),
                cancellationToken: cancellation))
                .Select(u => new PondRerquireParam()
                {
                    ParameterID = u.ParameterID,
                    ParameterName = u.Parameter.Name,
                    ParameterUnits = u.Parameter.ParameterUnits.Select(
                       u => new PondRerquireParamUnit()
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

        public async Task<PondResponse> UpdatePond(UpdatePondRequest request, CancellationToken cancellation)
        {
            var response = new PondResponse();
            var pond = await pondRepository.GetAsync(x => x.PondID.Equals(request.PondID), cancellation);
            if (pond != null)
            {
                pond.OwnerId = request.OwnerId;
                pond.CreateDate = request.CreateDate;
                pond.Name = request.Name;

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
    }
}
