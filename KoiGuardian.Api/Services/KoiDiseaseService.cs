using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using MongoDB.Bson.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KoiGuardian.Api.Services;

public interface IKoiDiseaseService
{
    Task<List<KoiDiseaseProfile>> GetDiseaseProfile();
    Task<List<KoiDiseaseProfile>> GetDisease( Guid fishId);
    Task<string> CreateProfile( DiseaseProfileRequest request);
}

public class KoiDiseaseService
    (
    IRepository<KoiDiseaseProfile> profileRepo,
    IUnitOfWork<KoiGuardianDbContext> unitOfWork
    )
    : IKoiDiseaseService
{
    public async Task<string> CreateProfile(DiseaseProfileRequest request)
    {
        try
        {
            var data = new KoiDiseaseProfile
            {
                KoiDiseaseProfileId = Guid.NewGuid(),
                DiseaseID = request.DiseaseID,
                MedicineId = request.MedicineId,
                FishId = request.FishId,
                Createddate = DateTime.Now,
                EndDate = request.EndDate,
                Status = (ProfileStatus)Enum.Parse(typeof(ProfileStatus), request.Status),
                Symptoms = JsonSerializer.Serialize(request.Symptoms),
                Note = request.Note,
            };

            profileRepo.Insert(data);
            await unitOfWork.SaveChangesAsync();
            return "Create successfully!";
        } catch (Exception ex) { 
            return "Create fail: " + ex.Message;
        }

    }

    public async Task<List<KoiDiseaseProfile>> GetDisease(Guid fishId)
    {
        return (await profileRepo.FindAsync( u => u.FishId == fishId,
            orderBy: u => u.OrderByDescending(u => u.Createddate))).ToList();
    }

    public async Task<List<KoiDiseaseProfile>> GetDiseaseProfile()
    {
        return (await profileRepo.GetAllAsync()).ToList();
    }
}
