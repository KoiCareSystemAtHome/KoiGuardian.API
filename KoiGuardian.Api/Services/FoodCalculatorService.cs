using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.Api.Services;

public interface IFoodCalculatorService
{
    Task<CalculateFoodResponse> Calculate(CalculateFoodRequest req);
    Task<object> Suggest(Guid id);
}

public class FoodCalculatorService
    (
        IRepository<NormFoodAmount> normFoodAmountRepository,
        IRepository<Pond> pondRepository,
        IRepository<KoiDiseaseProfile> koiProfileRepository,
        IRepository<Food> foodRepository,
        IRepository<KoiReport> koiParamRepository
    )
    : IFoodCalculatorService
{
    private Dictionary<string, float> DesiredGrowthPercent = new Dictionary<string, float>
        {
            { "Low", 0.5f },
            { "Medium", 1f },
            { "High", 1.5f }
        };

    public async Task<CalculateFoodResponse> Calculate(CalculateFoodRequest req)
    {
        var pond = await pondRepository.GetAsync(
            u => u.PondID == req.PondId,
            include: u => u.Include(u => u.Fish));

        if (pond == null || pond.Fish == null || !pond.Fish.Any())
        {
            return new CalculateFoodResponse()
            {
                AddtionalInstruction = [" No fish found in this pond"]
            };
        }

        var foodTotal = 0f;
        var often = new List<string>();
        var noteList = new List<string>();
        float totalweight = 0;
        foreach (var koi in pond.Fish)
        {
            var koiPercent = 0f;
            var normPercents = await normFoodAmountRepository.FindAsync(
                u => u.AgeFrom <= koi.Age && u.AgeTo >= koi.Age
                && req.TemperatureUpper <= u.Temperature,
                CancellationToken.None);
            var normPercent = normPercents.OrderByDescending(u => u.Temperature).FirstOrDefault();
            if (normPercent != null) { 
                koiPercent = koiPercent + normPercent.StandardAmount;
                often.Add(normPercent.FeedingFrequency);
            }
            var test = await koiProfileRepository.GetAllAsync();
            var treatmentAmount = await koiProfileRepository.GetQueryable().Where(
                 u => koi.KoiID == koi.KoiID
                 && u.EndDate <= DateTime.UtcNow).Include(u => u.Disease).FirstOrDefaultAsync();

            if (treatmentAmount != null)
            {
                koiPercent = koiPercent + (treatmentAmount?.Disease?.FoodModifyPercent ?? 0f);
                noteList.Add(koi.Name + " đang điều trị bệnh " + treatmentAmount.Disease.Name);
            }

            var koiweight = await koiParamRepository.GetQueryable()
                .OrderByDescending(u=> u.CalculatedDate).FirstOrDefaultAsync();
            totalweight += koiweight.Weight;
            foodTotal = foodTotal + (koiPercent * koiweight?.Weight ?? 0);
            
        }

        DesiredGrowthPercent.TryGetValue(req.DesiredGrowth, out var growth);

        if (growth != null && growth > 0)
        {
            foodTotal = foodTotal * growth;
        }


        return new CalculateFoodResponse()
        {
            FoodAmount = foodTotal,
            FeedingOften = often
                .GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? "2/3 lần 1 ngày",
            NumberOfFish = pond.Fish.Count(),
            TotalFishWeight  = totalweight,
            AddtionalInstruction = noteList
        };
    }

    public async Task<object> Suggest(Guid id)
    {
        var pond = await pondRepository.GetAsync(u => u.PondID == id,
            include : u=> u.Include(u => u.Fish));

        var minFishAge = int.MaxValue;
        var fishDateSince = int.MaxValue;

        foreach (var fish in pond.Fish)
        {
            int daysInPond = (DateTime.Now - fish.InPondSince).Days;

            if (daysInPond < fishDateSince)
            {
                fishDateSince = daysInPond;
            }

            if (fish.Age < minFishAge)
            {
                minFishAge = fish.Age;
            }
        }

        var food = await foodRepository.FindAsync( u => u.AgeFrom < minFishAge,
            include: u => u.Include(u => u.Product));

        if (food == null || food.Count() == 0)
        {
            food = await foodRepository.FindAsync(u => u.AgeTo > minFishAge,
                include: u => u.Include(u => u.Product));
        }

        string note = "";
        if (food == null || food.Count() == 0)
        {
            if( minFishAge < 90)
            {
                note = "Cá còn quá nhỏ, nên cho các loại thức ăn nhỏ như giun, bột cám,...";
            }else if (minFishAge < 180)
            {
                note = "Cá đang trong quá trình phát triển," +
                    " nên cho các loại thức ăn tăng trưởng...";
            }
            else 
            {
                note = "Cá đã phát triển," +
                    " có thể cho cá ăn đa dạng nguồn thức ăn...";
            }

        }

        if (food!= null && fishDateSince < 30)
        {
            note = note + " \n Có cá vừa vào hồ, thích hợp ăn thức ăn chìm.";
            food = (IList<Food>?) food.Where(u => u.Product.FoodIsFloat == true);
        }

        return new
        {
            Foods = food,
            Note = note
        };
    }
}


