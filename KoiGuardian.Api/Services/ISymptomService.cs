using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.Api.Services;

public interface ISymptomService
{
    Task<FinalDiseaseTypePredictResponse> Examination(List<DiseaseTypePredictRequest> symptoms);
    Task<FinalDiseaseTypePredictResponse> Examination2();
    Task<DiseaseTypePredictResponse> DiseaseTypePredict(List<DiseaseTypePredictRequest> symptoms);
    Task<List<PredictSymptoms>> GetByType(string? type);
    Task<string> Reminder(Guid pondId);
}

public class SymptomService( 
    IRepository<PredictSymptoms> predictSymptomRepository,
    IRepository<Symptom> symptomRepository,
    IRepository<Disease> diseaseRepository,
    IRepository<RelPredictSymptomDisease> relPredictSymptomDiseaseRepository,
    IRepository<RelSymptomDisease> relSymptomDiseaseRepository,
    IRepository<PondReminder> reminderRepository,
    IUnitOfWork<KoiGuardianDbContext> uow

    ) : ISymptomService
{
    public async Task<DiseaseTypePredictResponse> DiseaseTypePredict(List<DiseaseTypePredictRequest> symptoms)
    {
        var symptomDatas = await predictSymptomRepository.FindAsync( 
            u => symptoms.Select(u => u.SymtompId).Contains(u.SymtompId));
        var groupDatas = new Dictionary<string, int>();
        foreach (var symptomData in symptomDatas)
        {
            var reqSymp = symptoms.First( u => u.SymtompId == symptomData.SymtompId);
            if (groupDatas.TryGetValue(symptomData.Type, out var type))
            {

            }
            else
            {
             groupDatas.Add(symptomData.Type, 0);

            }


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
        var causeGroupType = typeFinal.Key.Split('_').Last().ToLower();

        var symptompReturn = await predictSymptomRepository.FindAsync(u => u.Type.ToLower() == causeGroupType);
        if (typeFinal.Value < 2) {
            return new DiseaseTypePredictResponse()
            {
                CauseGroupType = "Cá ăn quá no",
                SymptomPredicts = []
            };
        }
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
        var relations = await relPredictSymptomDiseaseRepository
            .FindAsync( u => symptoms.Select(u => u.SymtompId).Contains( u.PredictSymptomsId),
                include: u=> u.Include(u => u.PredictSymptoms)
            , CancellationToken.None);

        var groupData = relations.GroupBy( u => u.DiseaseId);
        Guid? predictDiseaseId = null;
        var maxpoint = 0;

        foreach (var rel in groupData) {
            var currentPoint = 0;
            foreach (var symptomData in rel)
            {
                var reqSymp = symptoms.FirstOrDefault( u => u.SymtompId == symptomData.PredictSymptomsId);
                if(symptomData.PredictSymptoms == null)
                {
                    continue;
                }
                if (Enum.TryParse<SymptomUnit>(symptomData.PredictSymptoms?.SymptomUnit, out var unit))
                {
                    switch (unit)
                    {
                        case SymptomUnit.check:
                            if (reqSymp.Value.ToLower() == "true")
                            {
                                if (symptomData.PredictSymptoms.SymptomPriority)
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
                                if (rate >= 3 && symptomData.PredictSymptoms.SymptomPriority)
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
                                    && symptomData.PredictSymptoms.SymptomPriority)
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
            u => u.DiseaseId == predictDiseaseId.Value, CancellationToken.None);

        return new FinalDiseaseTypePredictResponse()
        {
            CauseGroupType = relations.First().PredictSymptoms.Type ,
            DiseaseId = predictDiseaseId ?? Guid.Empty,
            DiseaseName =  predictDisease.Name,
            Description = predictDisease.Description,
        };
    }

    public async Task<FinalDiseaseTypePredictResponse> Examination2()
    {

        var diseaseList = await diseaseRepository.GetAllAsync();
        var symptomList = await symptomRepository.GetAllAsync();
        var random = new Random();

        foreach (var d in diseaseList)
        {
            // Lấy 3 symptom ngẫu nhiên, không trùng lặp
            var randomSymptomSample = symptomList
                .OrderBy(x => random.Next())
                .Take(3)
                .ToList();

            foreach (var s in randomSymptomSample)
            {
                relSymptomDiseaseRepository.Insert(new RelSymptomDisease()
                {
                    DiseaseId = d.DiseaseId,
                    SymptomSymtompId = s.SymtompId,
                    RelSymptomDiseaseId = Guid.NewGuid(),
                    DiseaseLower = 0,
                    DiseaseUpper = 100
                });
            }
        }

        await uow.SaveChangesAsync();


        return new();
    }

    public async Task<List<PredictSymptoms>> GetByType(string? type)
    {
        type = type ?? SymptomType.Common.ToString();
        if (type.ToLower() == SymptomType.Common.ToString().ToLower())
        {
            return (await predictSymptomRepository
            .FindAsync(u => u.Type.Contains(type ?? SymptomType.Common.ToString()), CancellationToken.None)).ToList();
        }

        return (await predictSymptomRepository
            .FindAsync(u => u.Type.ToLower()  == type.ToLower(), CancellationToken.None)).ToList();
    }

    public async Task<string> Reminder(Guid pondId)
    {
        try
        {
            reminderRepository.Insert(new PondReminder()
            {
                PondId = pondId,
                Description = " Kiểm tra các thông số của hồ để cá luôn khỏe mạnh",
                MaintainDate = DateTime.UtcNow.AddDays(1),
                ReminderType = ReminderType.Pond,
                PondReminderId = Guid.NewGuid(),
                SeenDate = DateTime.MinValue.ToUniversalTime(),
                Title = "Phòng bệnh cho cá"
            });
            await uow.SaveChangesAsync();
            return "";
        } catch (Exception ex) {
            return ex.Message;
        }
    }
}