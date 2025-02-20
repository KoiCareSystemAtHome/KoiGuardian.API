using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KoiGuardian.Api.Services;

public interface IKoiDiseaseService
{
    Task<List<KoiDiseaseProfile>> GetDiseaseProfile();
    Task<List<KoiDiseaseProfile>> GetDisease( Guid fishId);
    Task<string> CreateProfile( DiseaseProfileRequest request);

    Task<RecommendResponse> GetMedicineRecommendationsForFish(Guid fishId);

}

public class KoiDiseaseService
    (
    IRepository<KoiDiseaseProfile> profileRepo,
    IRepository<Medicine> medicineRepo,
    IRepository<MedicineDisease> medicineDiseaseRepo,
    IRepository<Disease> diseaseRepo,
    IRepository<Feedback> feedbackRepo,
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
                Createddate = DateTime.UtcNow,
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

    public async Task<RecommendResponse> GetMedicineRecommendationsForFish(Guid fishId)
    {
        try
        {
            // Get active disease profiles for the fish
            var activeProfiles = await profileRepo.FindAsync(
                p => p.FishId == fishId && p.Status == ProfileStatus.Pending,
                orderBy: q => q.OrderByDescending(p => p.Createddate)
            );

            if (!activeProfiles.Any())
            {
                return new RecommendResponse();
            }

            var recommendations = new List<MedicineResponse>();

            foreach (var profile in activeProfiles)
            {
                if (profile.DiseaseID == null) continue;

                // Get all medicine-disease relationships for this disease
                var medicineDiseases = await medicineDiseaseRepo.FindAsync(
                    md => md.DiseaseId == profile.DiseaseID
                );

                foreach (var medicineDisease in medicineDiseases)
                {
                    // Get medicine details with product information
                    var medicine = await medicineRepo.GetQueryable()
                        .Include(m => m.Product)
                        .FirstOrDefaultAsync(m => m.MedicineId == medicineDisease.MedinceId);

                    if (medicine?.Product == null) continue;


                    var feedbacks = await feedbackRepo.FindAsync(f => f.ProductId == medicine.ProductId);

                    var avgRating = feedbacks.Any() ? feedbacks.Average(f => f.Rate) : 0;
                    var feedbackCount = feedbacks.Count();

                    var medicineResponse = new MedicineResponse
                    {
                        MedicineId = medicine.MedicineId,
                        Medicinename = medicine.Medicinename,
                        DosageForm = medicine.DosageForm,
                        Symtomps = medicine.Symtomps,
                        Price = medicine.Product.Price,
                        StockQuantity = medicine.Product.StockQuantity,
                        FeedbackCount = feedbackCount,
                        AverageRating = avgRating
                    };

                    recommendations.Add(medicineResponse);
                }
            }

            // Return deduplicated recommendations
            return new RecommendResponse
            {
                Medicines = recommendations
                .GroupBy(r => r.MedicineId)
                .Select(g => g.First())
                .OrderByDescending(m => m.AverageRating) // Sắp xếp theo rating trung bình
                .ToList()
            };
        }
        catch (Exception ex)
        {
            // Log the exception here
            Console.WriteLine($"Error in GetMedicineRecommendationsForFish: {ex.Message}");
            return new RecommendResponse();
        }
    }





}


