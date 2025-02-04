using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.Api.Services;

public interface ISymptomService
{
    Task<FinalDiseaseTypePredictResponse> Examination(List<DiseaseTypePredictRequest> symptoms);
    Task<DiseaseTypePredictResponse> DiseaseTypePredict(List<DiseaseTypePredictRequest> symptoms);
    Task<List<Symptom>> GetByType(string? type);
}

public class SymptomService( 
    IRepository<Symptom> symptomRepository,
    IRepository<Disease> diseaseRepository,
    IRepository<RelSymptomDisease> relSymptomDiseaseRepository

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

    public async Task<FinalDiseaseTypePredictResponse> Examination(List<DiseaseTypePredictRequest> symptoms)
    {
        var relations = await relSymptomDiseaseRepository
            .FindAsync( u => symptoms.Select(u => u.SymtompId).Contains( u.SymtompId),
                include: u=> u.Include(u => u.Symptom)
            , CancellationToken.None);

        var groupData = relations.GroupBy( u => u.DiseaseId);
        Guid? predictDiseaseId = null;
        var maxpoint = 0;

        foreach (var rel in groupData) {
            var currentPoint = 0;
            foreach (var symptomData in rel)
            {
                var reqSymp = symptoms.FirstOrDefault( u => u.SymtompId == symptomData.SymtompId);
                if (Enum.TryParse<SymptomUnit>(symptomData.Symptom.SymptomUnit, out var unit))
                {
                    switch (unit)
                    {
                        case SymptomUnit.check:
                            if (reqSymp.Value.ToLower() == "true")
                            {
                                if (symptomData.Symptom.SymptomPriority)
                                {
                                    currentPoint = currentPoint + 2;
                                }
                                else
                                {
                                    currentPoint = currentPoint + 1;
                                }
                            }
                            break;

                        case SymptomUnit.rate:
                            if (int.TryParse(reqSymp.Value.ToLower(), out var rate))
                            {
                                if (rate >= 3 && symptomData.Symptom.SymptomPriority)
                                {
                                    currentPoint = currentPoint + 2;
                                }
                                else if (rate >= 3)
                                {
                                    currentPoint = currentPoint + 1;
                                }
                            }
                            break;

                        case SymptomUnit.range:
                            if (float.TryParse(reqSymp.Value.ToLower(), out var range))
                            {
                                if (range >= symptomData.DiseaseUpper && range <= symptomData.DiseaseLower
                                    && symptomData.Symptom.SymptomPriority)
                                {
                                    currentPoint = currentPoint + 2;
                                }
                                else if (range >= symptomData.DiseaseUpper && range <= symptomData.DiseaseLower)
                                {
                                    currentPoint = currentPoint + 1;
                                }
                            }
                            break;

                        default:
                            break;

                    }
                }
            }

            if (currentPoint> maxpoint)
            {
                predictDiseaseId = rel.Key;
            }
        }

        if (predictDiseaseId == null)
        {
            return new();
        }

        var predictDisease = await diseaseRepository.GetAsync(
            u => u.DiseaseId == predictDiseaseId,
            include: u=> u.Include(u => u.Medicine));

        return new FinalDiseaseTypePredictResponse()
        {
            CauseGroupType = relations.First().Symptom.Type ,
            DiseaseId = predictDiseaseId ?? Guid.Empty,
            DiseaseName =  predictDisease.Name,
            Description = predictDisease.Description,
            Medicine = predictDisease.Medicine?.Select( u => new MedicinePredict()
            {
                MedicineId = u.MedicineId,
                Instruction = u.Instruction,
                Data = u.Data
            })
        };
    }

    public async Task<List<Symptom>> GetByType(string? type)
    {
        type = type ?? SymptomType.Common.ToString();
        if (type.ToLower() == SymptomType.Common.ToString().ToLower())
        {
            return (await symptomRepository
            .FindAsync(u => u.Type.Contains(type ?? SymptomType.Common.ToString()), CancellationToken.None)).ToList();
        }

        return (await symptomRepository
            .FindAsync(u => u.Type.ToLower()  == type.ToLower(), CancellationToken.None)).ToList();
    }


}