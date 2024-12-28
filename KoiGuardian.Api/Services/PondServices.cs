using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.DataAccess;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;

namespace KoiGuardian.Api.Services
{
    public interface IPondServices 
    {
        Task<PondResponse> CreatePond(PondRequest Request, CancellationToken cancellation);
        Task<PondResponse> UpdatePond(PondRequest Request, CancellationToken cancellation);
    }

    public class PondServices(
        IRepository<Pond> pondRepository, 
        KoiGuardianDbContext _dbContext, 
        IRepository<User> userRepository) : IPondServices
    {
        public async Task<PondResponse> CreatePond(PondRequest Request, CancellationToken cancellation)
        {
            var Response = new PondResponse();
            //kiểm tra hồ đã tồn tại chưa
            var pond = await pondRepository.GetAsync(x => x.PondID.Equals(Request.PondID), cancellation);
            //kiểm tra user đang đăng nhập
            /*var user = await userRepository.GetAsync( x => x.Id.Equals(Request.OwnerId), cancellation);

            if(user is null)
            {
                Response.status = "404";
                Response.message = "User does not exist";
                return Response;
            }*/

            if(pond is null) 
            {
                pond = new Pond()
                {
                    //PondID = Request.PondID,
                    OwnerId = Request.OwnerId,
                    CreateDate = Request.CreateDate,
                    Name = Request.Name,
                };

                pondRepository.Insert(pond);
                await _dbContext.SaveChangesAsync(cancellation);

                Response.status = "201";
                Response.message = "Create Ponnd Success";
            }
            else
            {
                Response.status = "409 ";
                Response.message = "Pond Has Existed";
            }
            return Response;
        }

        public async Task<PondResponse> UpdatePond(PondRequest Request, CancellationToken cancellation)
        {
            var Response = new PondResponse();
            var pond = await pondRepository.GetAsync(x => x.PondID.Equals(Request.PondID), cancellation);
            if (pond != null)
            {
                pond.OwnerId = Request.OwnerId;
                pond.CreateDate = Request.CreateDate;
                pond.Name = Request.Name;
                

                pondRepository.Update(pond);
                await _dbContext.SaveChangesAsync(cancellation);

                Response.status = "201";
                Response.message = "Update Ponnd Success";
            }
            else
            {
                Response.status = "409 ";
                Response.message = "Pond Haven't Existed";
            }
            return Response;
        }
    }
}
