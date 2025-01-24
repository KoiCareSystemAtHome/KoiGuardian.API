using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.Api.Services;

public interface IFoodCalculatorService
{
    Task<CalculateFoodResponse> Calculate(CalculateFoodRequest req);
}

public class FoodCalculatorService
    (
        IRepository<NormFoodAmount> normFoodAmountRepository,
        IRepository<Pond> pondRepository,
        IRepository<KoiDiseaseProfile> koiProfileRepository,
        IRepository<RelKoiParameter> koiParamRepository
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

        foreach (var koi in pond.Fish)
        {
            var koiPercent = 0f;
            var normPercent = await normFoodAmountRepository.GetAsync(
                u => u.AgeFrom <= koi.Age && u.AgeTo >= koi.Age
                && req.TemperatureLower >= u.TemperatureLower
                && req.TemperatureUpper <= u.TemperatureUpper,
                CancellationToken.None);
            koiPercent = koiPercent + normPercent.StandardAmount;

            var treatmentAmount = await koiProfileRepository.GetAsync(
                 u => koi.KoiID == koi.KoiID
                 && u.EndDate <= DateTime.Now
                 ,
                include: u => u.Include(u => u.Disease));

            if (treatmentAmount != null)
            {
                koiPercent = koiPercent + (treatmentAmount?.Disease?.FoodModifyPercent ?? 0f);
                noteList.Add(koi.Name + " đang điều trị bệnh " + treatmentAmount.Disease.Name);
            }

            var koiweight = await koiParamRepository.GetAsync(
                u => u.Parameter.Name.ToLower() == "weight",
                include: u => u.Include(u => u.Parameter));

            foodTotal = foodTotal + (koiPercent * koiweight?.Value ?? 0);
            often.Add(normPercent.FeedingOften);
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
                .FirstOrDefault()?.Key ?? "",
            AddtionalInstruction = noteList
        };
    }
}


