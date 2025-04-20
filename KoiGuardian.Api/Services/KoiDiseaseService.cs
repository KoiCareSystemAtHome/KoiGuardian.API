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

    Task<List<KoiDiseaseProfile>> GetActiveDiseaseProfiles();
    Task<string> UpdateProfile(Guid profileId, UpdateDiseaseProfileRequest request, CancellationToken cancellationToken);




}

public class KoiDiseaseService
    (
    IRepository<KoiDiseaseProfile> profileRepo,
    IRepository<Medicine> medicineRepo,
    IRepository<MedicineDisease> medicineDiseaseRepo,
    IRepository<Disease> diseaseRepo,
    IRepository<Feedback> feedbackRepo,
    IRepository<PondReminder> reminderRepo,
    IRepository<Fish> fishRepo,
    IUnitOfWork<KoiGuardianDbContext> unitOfWork
    )
    : IKoiDiseaseService
{
    public async Task<string> CreateProfile(DiseaseProfileRequest request)
    {
        try
        {
            var fish = (await fishRepo.FindAsync(u => u.KoiID == request.FishId))
                .FirstOrDefault();

            if (fish == null) { return "Fish not found"; }

            var data = new KoiDiseaseProfile
            {
                KoiDiseaseProfileId = Guid.NewGuid(),
                DiseaseId = request.DiseaseID,
                MedicineId = request.MedicineId ?? Guid.Empty,
                FishId = request.FishId,
                Createddate = DateTime.UtcNow,
                EndDate = request.EndDate,
                Status = (ProfileStatus)Enum.Parse(typeof(ProfileStatus), request.Status),
                Symptoms = JsonSerializer.Serialize(request.Symptoms),
                Note = request.Note,
            };


            profileRepo.Insert(data);

            reminderRepo.Insert( new PondReminder()
            {
                PondId = fish.PondID,
                PondReminderId = Guid.NewGuid(),
                ReminderType = ReminderType.Pond,
                Title = "Kiểm tra sức khỏe cá ", 
                Description = " Kiểm tra sức khỏe cá sau khi dùng thuốc",
                MaintainDate =request.EndDate,
                SeenDate = request.EndDate,
            });

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
                if (profile.DiseaseId == null) continue;

                // Get all medicine-disease relationships for this disease
                var medicineDiseases = await medicineDiseaseRepo.FindAsync(
                    md => md.DiseaseId == profile.DiseaseId
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
                        MedicineName = medicine.Medicinename,
                        DosageForm = medicine.DosageForm,
                        Symptoms = medicine.Symtomps,
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
    public async Task<List<KoiDiseaseProfile>> GetActiveDiseaseProfiles()
    {
        try
        {
            
            var activeProfiles = await profileRepo.GetQueryable()
                .Where(p => p.Status == ProfileStatus.Pending)
                .OrderByDescending(p => p.Createddate)
                .ToListAsync();

            return activeProfiles;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetActiveDiseaseProfiles: {ex.Message}");
            return new List<KoiDiseaseProfile>();
        }
    }

    public async Task<string> UpdateProfile(Guid profileId, UpdateDiseaseProfileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var profile = await profileRepo.GetAsync(x => x.KoiDiseaseProfileId == profileId, cancellationToken);
            if (profile == null)
                return "Profile not found";

            // Cập nhật trạng thái nếu có
            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ProfileStatus>(request.Status, out ProfileStatus status))
            {
                profile.Status = status;
                profile.EndDate = status == ProfileStatus.Buyed ? DateTime.UtcNow : profile.EndDate;
            }

            // Cập nhật thuốc nếu có
            if (request.MedicineId.HasValue)
            {
                profile.MedicineId = request.MedicineId.Value;
            }
            if (request.DiseaseID.HasValue)
            {
                profile.DiseaseId = request.DiseaseID.Value;
            }

            // Cập nhật ghi chú nếu có
            if (!string.IsNullOrEmpty(request.Note))
            {
                profile.Note = request.Note;
            }

            if (request.EndDate != null)
            {
                profile.EndDate = request.EndDate?? DateTime.Now;
            }

            // Cập nhật triệu chứng nếu có
            if (request.Symptoms != null && request.Symptoms.Any())
            {
                profile.Symptoms = JsonSerializer.Serialize(request.Symptoms);
            }

            profileRepo.Update(profile);
            await unitOfWork.SaveChangesAsync();

            return "Profile updated successfully!";
        }
        catch (Exception ex)
        {
            return "Update failed: " + ex.Message;
        }
    }








}


