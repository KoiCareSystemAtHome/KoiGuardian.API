using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.DataAccess;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using System.Reflection.Metadata;
using System.Threading;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.Api.Services
{

    public interface IDiseaseService
    {
         Task<DiseaseResponse> CreateDisease(CreateDiseaseRequest request, CancellationToken cancellationToken);
         Task<DiseaseResponse> UpdateDisease(UpdateDiseaseRequest request, CancellationToken cancellationToken);
         Task<DiseaseResponse> DeleteDisease(Guid diseaseId, CancellationToken cancellationToken);
         Task<List<DiseaseResponse>> GetAllDiseases(CancellationToken cancellationToken = default);
         Task<DiseaseResponse> GetDiseaseById(Guid diseaseId, CancellationToken cancellationToken);
        Task<FinalDiseaseTypePredictResponse> Examination(List<DiseaseTypePredictRequest> symptoms);
        Task<DiseaseTypePredictResponse> DiseaseTypePredict(List<DiseaseTypePredictRequest> symptoms);
        Task<List<PredictSymptoms>> GetByType(string? type);
        Task<object> sideEffects();
        Task<object> sickSymtomps();
    }


    public class DiseaseService(IRepository<Disease> _diseaseRepository,
            IRepository<Medicine> _medicineRepository,
            IRepository<MedicineDisease> _medicineDiseaseRepository,
            IRepository<PredictSymptoms> _predictSymtopmsRepository,
            IRepository<Product> _productsRepository,
            IRepository<Symptom> _symtopmsRepository,
            IRepository<RelSymptomDisease> _relsymptompdeseaseSymtopmsRepository,
            IRepository<RelPredictSymptomDisease> _relpredictSymtopmsDiseaseRepository,
            IUnitOfWork<KoiGuardianDbContext> _unitOfWork) : IDiseaseService
    {
       

        public async Task<DiseaseResponse> CreateDisease(CreateDiseaseRequest request, CancellationToken cancellation)
        {
            var response = new DiseaseResponse();

            var disease = new Disease
            {
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,  // Direct enum assignment
                FoodModifyPercent = request.FoodModifyPercent,
                SaltModifyPercent = request.SaltModifyPercent,
                Image = request.Image

            };

            _diseaseRepository.Insert(disease);

            if (request.MedicineIds?.Any() == true)
            {
                foreach (var medicineId in request.MedicineIds)
                {
                    var medicineDisease = new MedicineDisease
                    {
                        MedinceId = medicineId,
                        DiseaseId = disease.DiseaseId
                    };
                    _medicineDiseaseRepository.Insert(medicineDisease);
                }
            }

            if (request.SideEffect?.Any() == true)
            {
                foreach (var id in request.SideEffect)
                {
                    var symptomp =( await _symtopmsRepository
                        .FindAsync( u => u.SymtompId == id)).FirstOrDefault();
                    var rel = new RelSymptomDisease
                    {
                        RelSymptomDiseaseId = Guid.NewGuid() ,
                        DiseaseId = disease.DiseaseId,
                        SymptomSymtompId = id,
                        DiseaseLower = 0,
                        DiseaseUpper = 100,
                        SymtompId = id
                    };
                    _relsymptompdeseaseSymtopmsRepository.Insert(rel);
                }
            }

            if (request.SickSymtomps?.Any() == true)
            {
                foreach (var id in request.SickSymtomps)
                {
                    var rel = new RelPredictSymptomDisease
                    {
                        RelSymptomDiseaseId = Guid.NewGuid(),
                        DiseaseId = disease.DiseaseId,
                        PredictSymptomsId = id,
                        DiseaseLower = 0,
                        DiseaseUpper = 100,
                    };
                    _relpredictSymtopmsDiseaseRepository.Insert(rel);
                }
            }

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellation);
                response.Status = "201";
                response.Message = "Disease created successfully.";
            }
            catch (Exception ex)
            {
                return new DiseaseResponse
                {
                    Status = "500",
                    Message = "Error creating disease: " + ex.Message
                };
            }

            return response;
        }

        public async Task<DiseaseResponse> UpdateDisease(UpdateDiseaseRequest request, CancellationToken cancellationToken)
        {
            var response = new DiseaseResponse();

            var diseaseExist = await _diseaseRepository.GetAsync(x => x.DiseaseId.Equals(request.DiseaseId), cancellationToken);

                diseaseExist.Name = request.Name;
                diseaseExist.Description = request.Description;
                diseaseExist.Type = request.Type;
                diseaseExist.FoodModifyPercent = request.FoodModifyPercent;
                diseaseExist.SaltModifyPercent = request.SaltModifyPercent;
            diseaseExist.Image = request.Image;

                _diseaseRepository.Update(diseaseExist);

            //var diseaseMedicineExist = diseaseExist.MedicineDiseases?.ToList() ?? new List<MedicineDisease>();

            //foreach (var diseaseMedicineExists in diseaseMedicineExist)
            //{
            //    _medicineDiseaseRepository.Delete(diseaseMedicineExists);
            //}
            if (request.MedicineIds?.Any() == true)
            {
                foreach (var medicineId in request.MedicineIds)
                {
                    var diseaseMedicine = new MedicineDisease
                    {
                        MedinceId = medicineId,
                        DiseaseId = diseaseExist.DiseaseId
                    };
                    _medicineDiseaseRepository.Insert(diseaseMedicine);
                }
            }
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                response.Status = "200";
                response.Message = "Blog updated successfully.";
            }


           
            catch (Exception ex)
            {
                return new DiseaseResponse
                {
                    Status = "500",
                    Message = "Error updating disease: " + ex.Message
                };
            }

            return response;
        }

        public async Task<DiseaseResponse> DeleteDisease(Guid diseaseId, CancellationToken cancellationToken)
        {
            var response = new DiseaseResponse();
            try
            {
                var disease = await _diseaseRepository.GetAsync(d => d.DiseaseId == diseaseId, cancellationToken);
                if (disease == null)
                {
                    response.Status = "404";
                    response.Message = "Disease not found";
                    return response;
                }

                _diseaseRepository.Delete(disease);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                response.Status = "200";
                response.Message = "Disease deleted successfully";
            }
            catch (Exception ex)
            {
                response.Status = "500";
                response.Message = $"An error occurred: {ex.Message}";
            }

            return response;
        }

        public async Task<List<DiseaseResponse>> GetAllDiseases(CancellationToken cancellationToken = default)
        {
            var diseases = await _diseaseRepository.GetQueryable()
                .Include(d => d.MedicineDisease)
                    .ThenInclude(md => md.Medince) 
                .ToListAsync(cancellationToken);

            return diseases.Select(disease => new DiseaseResponse
            {
                DiseaseId = disease.DiseaseId,
                Name = disease.Name,
                Description = disease.Description,
                Type = disease.Type,
                FoodModifyPercent = disease.FoodModifyPercent,
                SaltModifyPercent = disease.SaltModifyPercent,
                Image = disease.Image,
                Medicines = disease.MedicineDisease?.Select(md => new MedicineDTO
                {
                    MedicineId = md.Medince.MedicineId,
                    Name = md.Medince.Medicinename, 
                }).ToList<object>() ?? new List<object>()
            }).OrderBy(u => u.Name).ToList();
        }




        public async Task<DiseaseResponse> GetDiseaseById(Guid diseaseId, CancellationToken cancellationToken)
        {
            var disease = (await _diseaseRepository.
                FindAsync(d => d.DiseaseId == diseaseId, 
                include : u => u.Include( u => u.MedicineDisease).ThenInclude( u => u.Medince))
                ).FirstOrDefault();

            if (disease == null)
            {
                return new DiseaseResponse
                {
                    Status = "404",
                    Message = "Disease not found"
                };
            }
            var predictSymtoms = await _relsymptompdeseaseSymtopmsRepository.FindAsync(
                    u => u.DiseaseId == disease.DiseaseId);
            var sick = (await _relpredictSymtopmsDiseaseRepository
                    .FindAsync(u => u.DiseaseId == diseaseId,
                        include: u => u.Include(u => u.PredictSymptoms)))
                    .Select(u => new
                    {
                        Id = u.PredictSymptomsId,
                        DiseaseUpper = u.DiseaseUpper,
                        DiseaseLower = u.DiseaseLower,
                        Description = u.PredictSymptoms?.Name ?? ""
                    }).ToList();
            var effect = (
                    await _relsymptompdeseaseSymtopmsRepository
                    .FindAsync(u => u.DiseaseId == diseaseId,
                        include: u => u.Include(u => u.Symptom)))
                    .Select(u => new
                    {
                        Id = u.SymptomSymtompId,
                        DiseaseUpper = u.DiseaseUpper,
                        DiseaseLower = u.DiseaseLower,
                        Description = u.Symptom?.Name ?? ""
                    }).ToList();

            var products = await _productsRepository.FindAsync(u =>
            disease.MedicineDisease.Select(u => u.Medince.ProductId).Contains(u.ProductId));


            return new DiseaseResponse
            {
                DiseaseId = disease.DiseaseId,
                Name = disease.Name,
                Description = disease.Description,
                Type = disease.Type,
                FoodModifyPercent = disease.FoodModifyPercent,
                Image = disease?.Image ?? "",
                SaltModifyPercent = disease?.SaltModifyPercent ?? 0,
                Status = "200",
                Message = "Disease retrieved successfully",
                Medicines = disease?.MedicineDisease?.Select( u => new
                {
                    MedicineId = u.MedinceId,
                    ProductId = u.Medince?.ProductId,
                    Name = u.Medince?.Medicinename,
                    DosageForm = u.Medince?.DosageForm,
                    Image = products.Where( a => a.ProductId == u.Medince?.ProductId).FirstOrDefault()?.Image
                }),
                SickSymtomps =sick,
                SideEffect = effect,

            };
        }

        public async Task<DiseaseTypePredictResponse> DiseaseTypePredict(List<DiseaseTypePredictRequest> symptoms)
        {
            var symptomDatas = await _predictSymtopmsRepository.FindAsync(
                u => symptoms.Select(u => u.SymtompId).Contains(u.SymtompId));
            var groupDatas = new Dictionary<string, int>();
            foreach (var symptomData in symptomDatas)
            {
                var reqSymp = symptoms.First(u => u.SymtompId == symptomData.SymtompId);
                if (groupDatas.TryGetValue(symptomData.Type, out var type))
                {

                }

                groupDatas.Add(symptomData.Type, 0);

                if (Enum.TryParse<SymptomUnit>(symptomData.SymptomUnit, out var unit))
                {
                    switch (unit)
                    {
                        case SymptomUnit.check:
                            if (reqSymp.Value.ToLower() == "true")
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
                                if (rate >= 3 && symptomData.SymptomPriority)
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

            var symptompReturn = await _predictSymtopmsRepository.FindAsync(u => u.Type.ToLower() == causeGroupType);
            return new DiseaseTypePredictResponse()
            {
                CauseGroupType = causeGroupType,
                SymptomPredicts = symptompReturn.Select(u => new SymptomPredict()
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
            var relations = await _relpredictSymtopmsDiseaseRepository
                .FindAsync(u => symptoms.Select(u => u.SymtompId).Contains(u.PredictSymptomsId),
                    include: u => u.Include(u => u.PredictSymptoms)
                , CancellationToken.None);

            var groupData = relations.GroupBy(u => u.DiseaseId);
            Guid? predictDiseaseId = null;
            var maxpoint = 0;

            foreach (var rel in groupData)
            {
                var currentPoint = 0;
                foreach (var symptomData in rel)
                {
                    var reqSymp = symptoms.FirstOrDefault(u => u.SymtompId == symptomData.PredictSymptomsId);
                    if (Enum.TryParse<SymptomUnit>(symptomData.PredictSymptoms.SymptomUnit, out var unit))
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

                if (currentPoint > maxpoint)
                {
                    predictDiseaseId = rel.Key;
                }
            }

            if (predictDiseaseId == null)
            {
                return new();
            }

            var predictDisease = await _diseaseRepository.GetAsync(
                u => u.DiseaseId == predictDiseaseId, CancellationToken.None);

            return new FinalDiseaseTypePredictResponse()
            {
                CauseGroupType = relations.First().PredictSymptoms.Type,
                DiseaseId = predictDiseaseId ?? Guid.Empty,
                DiseaseName = predictDisease.Name,
                Description = predictDisease.Description
            };
        }

        public async Task<List<PredictSymptoms>> GetByType(string? type)
        {
            type = type ?? SymptomType.Common.ToString();
            if (type.ToLower() == SymptomType.Common.ToString().ToLower())
            {
                return (await _predictSymtopmsRepository
                .FindAsync(u => u.Type.Contains(type ?? SymptomType.Common.ToString()), CancellationToken.None)).ToList();
            }

            return (await _predictSymtopmsRepository
                .FindAsync(u => u.Type.ToLower() == type.ToLower(), CancellationToken.None)).ToList();
        }

        public async Task<object> sideEffects()
        {
            return (await _symtopmsRepository.GetAllAsync()).Select(
                u => new
                {
                    Id = u.SymtompId,
                    Name = u.Name
                }
                );
        }

        public async Task<object> sickSymtomps()
        {
            return (await _predictSymtopmsRepository.GetAllAsync()).Select(
                u => new
                {
                    Id = u.SymtompId,
                    Name = u.Name
                }
                );
        }
    }
}

