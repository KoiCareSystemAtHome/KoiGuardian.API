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
        IRepository<KoiDiseaseProfile> koiProfileReposiroty
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
            include : u => u.Include( u=> u.Fish));
        
        if(pond.Fish == null || !pond.Fish.Any())
        {
            return new CalculateFoodResponse() {
                AddtionalInstruction =" No fish found in this pond"
            };
        }

        var foodPercent = 0f;
        var often = new List<string>();

        foreach(var fish in pond.Fish)
        {
            var normPercent = await normFoodAmountRepository.GetAsync( 
                u => u.AgeFrom <= fish.Age && u.AgeTo >= fish.Age
                && req.TemperatureLower >= u.TemperatureLower
                && req.TemperatureUpper <= u.TemperatureUpper,
                CancellationToken.None);
            foodPercent = foodPercent + normPercent.StandardAmount;

            //var treatmentAmount = koiProfileReposiroty.GetAsync(
            //     u => fish.,
            //    include: u => u.Medicine);


        }


        return new CalculateFoodResponse()
        {

        };
    }
}


