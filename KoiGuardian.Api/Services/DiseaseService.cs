using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.DataAccess;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using System.Reflection.Metadata;
using System.Threading;
using KoiGuardian.Core.UnitOfWork;

namespace KoiGuardian.Api.Services
{

    public interface IDiseaseService
    {
        Task<DiseaseResponse> CreateDisease(CreateDiseaseRequest request, CancellationToken cancellationToken);
        /* Task<DiseaseResponse> UpdateDisease(UpdateDiseaseRequest request, CancellationToken cancellationToken);
         Task<DiseaseResponse> DeleteDisease(Guid diseaseId, CancellationToken cancellationToken);
         Task<List<DiseaseResponse>> GetAllDiseases(string? name = null, CancellationToken cancellationToken = default);
         Task<DiseaseResponse> GetDiseaseById(Guid diseaseId, CancellationToken cancellationToken);*/
    }


    public class DiseaseService : IDiseaseService
    {
        private readonly IRepository<Disease> _diseaseRepository;
        private readonly IRepository<Medicine> _medicineRepository;
        private readonly IRepository<MedicineDisease> _medicineDiseaseRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public DiseaseService(IRepository<Disease> diseaseRepository, IRepository<Medicine> medicineRepository, IRepository<MedicineDisease> medicindeDiseaseRepository, IUnitOfWork<KoiGuardianDbContext> unitOfWork)
        {
            _diseaseRepository = diseaseRepository;
            _medicineRepository = medicineRepository;
            _medicineDiseaseRepository = medicindeDiseaseRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<DiseaseResponse> CreateDisease(CreateDiseaseRequest request, CancellationToken cancellation)
        {
            var response = new DiseaseResponse();

            var disease = new Disease
            {
                Name = request.Name,
                Description = request.Description,
                Type = (KoiGuardian.DataAccess.Db.DiseaseType)request.Type, 
                FoodModifyPercent = request.FoodModifyPercent,
                SaltModifyPercent = request.SaltModifyPercent
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
                    Message = "Error creating blog: " + ex.Message
                };
            }

            return response;

        }
    }
}

