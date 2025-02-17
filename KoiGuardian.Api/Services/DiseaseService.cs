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
    }


    public class DiseaseService : IDiseaseService
    {
        private readonly IRepository<Disease> _diseaseRepository;
        private readonly IRepository<Medicine> _medicineRepository;
        private readonly IRepository<MedicineDisease> _medicineDiseaseRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public DiseaseService(
            IRepository<Disease> diseaseRepository,
            IRepository<Medicine> medicineRepository,
            IRepository<MedicineDisease> medicineDiseaseRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork)
        {
            _diseaseRepository = diseaseRepository;
            _medicineRepository = medicineRepository;
            _medicineDiseaseRepository = medicineDiseaseRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<DiseaseResponse> CreateDisease(CreateDiseaseRequest request, CancellationToken cancellation)
        {
            var response = new DiseaseResponse();

            var disease = new Disease
            {
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,  // Direct enum assignment
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
                    .ThenInclude(md => md.Medince) // Load dữ liệu từ bảng Medicine
                .ToListAsync(cancellationToken);

            return diseases.Select(disease => new DiseaseResponse
            {
                DiseaseId = disease.DiseaseId,
                Name = disease.Name,
                Description = disease.Description,
                Type = disease.Type,
                FoodModifyPercent = disease.FoodModifyPercent,
                SaltModifyPercent = disease.SaltModifyPercent,
                Medicines = disease.MedicineDisease?.Select(md => new MedicineDTO
                {
                    MedicineId = md.Medince.MedicineId,
                    Name = md.Medince.Medicinename, // Đảm bảo lấy đúng tên thuốc
                   
                }).ToList() ?? new List<MedicineDTO>() // Đảm bảo danh sách không bị null
            }).ToList();
        }



        public async Task<DiseaseResponse> GetDiseaseById(Guid diseaseId, CancellationToken cancellationToken)
        {
            var disease = await _diseaseRepository
                .GetQueryable()
                .FirstOrDefaultAsync(d => d.DiseaseId == diseaseId, cancellationToken);

            if (disease == null)
            {
                return new DiseaseResponse
                {
                    Status = "404",
                    Message = "Disease not found"
                };
            }

            return new DiseaseResponse
            {
                DiseaseId = disease.DiseaseId,
                Name = disease.Name,
                Description = disease.Description,
                Type = disease.Type,
                FoodModifyPercent = disease.FoodModifyPercent,
                SaltModifyPercent = disease.SaltModifyPercent,
                Status = "200",
                Message = "Disease retrieved successfully"
            };
        }

    }
}

