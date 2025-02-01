using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;

namespace KoiGuardian.Api.Services;

public interface ISymptomService
{
    Task<DiseaseTypePredictResponse> DiseaseTypePredict(List<DiseaseTypePredictRequest> symptoms);
    Task<List<Symptom>> GetByType(string? type);
}

public class SymptomService( 
    IRepository<Symptom> symptomRepository
    ) : ISymptomService
{
    public async Task<DiseaseTypePredictResponse> DiseaseTypePredict(List<DiseaseTypePredictRequest> symptoms)
    {
        var symptomDatas = await symptomRepository.FindAsync( 
            u => symptoms.Select(u => u.SymtompId).Contains(u.SymtompId));
        var groupDatas = new Dictionary<string, int>();
        foreach (var symptomData in symptomDatas)
        {
            var reqSymp = symptoms.First( u => u.SymtompId == symptomData.SymtompId);
            if (groupDatas.TryGetValue(symptomData.Type, out var type))
            {

            }

            groupDatas.Add(symptomData.Type, 0);

            if (Enum.TryParse<SymptomUnit>(symptomData.SymptomUnit, out var unit))
            {
                switch (unit)
                {
                    case SymptomUnit.check:
                        if( reqSymp.Value.ToLower() == "true")
                        {
                            if (symptomData.SymptomPriority)
                            {
                                groupDatas[symptomData.Type] = groupDatas[symptomData.Type] + 2;
                            }
                            else
                            {
                                groupDatas[symptomData.Type] = groupDatas[symptomData.Type] + 1;
                            }
                        }
                        break;

                    case SymptomUnit.rate:
                        if (int.TryParse(reqSymp.Value.ToLower(), out var rate))
                        {
                            if (rate >=3 && symptomData.SymptomPriority)
                            {
                                groupDatas[symptomData.Type] = groupDatas[symptomData.Type] + 2;
                            }
                            else if (rate >= 3)
                            {
                                groupDatas[symptomData.Type] = groupDatas[symptomData.Type] + 1;
                            }
                        }
                        break;

                    default:
                        break;

                }
            }
        }
        var typeFinal = groupDatas.OrderByDescending(u => u.Value).FirstOrDefault();
        var causeGroupType = typeFinal.Key.Split('-').Last().ToLower();

        var symptompReturn = await symptomRepository.FindAsync(u => u.Type.ToLower() == causeGroupType);
        return new DiseaseTypePredictResponse()
        {
            CauseGroupType = causeGroupType,
            SymptomPredicts = symptompReturn.Select( u => new SymptomPredict()
            {
                SymtompId = u.SymtompId,
                Name = u.Name,
                SymptomPriority = u.SymptomPriority,
                SymptomUnit = u.SymptomUnit
            }).ToList()
        };
    }

    public async Task<List<Symptom>> GetByType(string? type)
    {
        return (await symptomRepository
            .FindAsync( u => u.Type.Contains(type ?? SymptomType.Common.ToString()), CancellationToken.None)).ToList();
    }


}