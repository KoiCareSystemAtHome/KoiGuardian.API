using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.DataAccess;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Azure.Core;
using Azure;

namespace KoiGuardian.Api.Services
{
    public interface IPondServices 
    {
        Task<PondResponse> CreatePond(CreatePondRequest Request, CancellationToken cancellation);
        Task<PondResponse> UpdatePond(UpdatePondRequest Request, CancellationToken cancellation);
    }

    public class PondServices(
        IRepository<Pond> pondRepository, 
        KoiGuardianDbContext _dbContext, 
        IRepository<User> userRepository) : IPondServices
    {
        public async Task<PondResponse> CreatePond(CreatePondRequest request, CancellationToken cancellation)
        {
            var response = new PondResponse();
            var pond = new Pond
            {
                PondID = Guid.NewGuid(),
                OwnerId = request.OwnerId,
                CreateDate = request.CreateDate,
                Name = request.Name
            };

            try
            {
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
