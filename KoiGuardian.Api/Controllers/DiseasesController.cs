using KoiGuardian.Api.Services;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiseasesController : ControllerBase
    {
        private IDiseaseService _service;

        public DiseasesController(IDiseaseService service)
        {
            _service = service;
        }

        [HttpPost("create")]
        public async Task<DiseaseResponse> CreateDiseases([FromBody] CreateDiseaseRequest request, CancellationToken cancellation)
        {
            return await _service.CreateDisease(request, cancellation);
        }

        [HttpPut("update")]
        public async Task<DiseaseResponse> UpdateDisease([FromBody] UpdateDiseaseRequest request, CancellationToken cancellation)
        {
            return await _service.UpdateDisease(request, cancellation);
        }
        [HttpDelete("delete")]
        public async Task<DiseaseResponse> DeleteDisease([FromQuery] Guid diseaseId, CancellationToken cancellationToken)
        {
            return await _service.DeleteDisease(diseaseId, cancellationToken);
        }

        [HttpGet("all-disease")]
        public async Task<IList<DiseaseResponse>> GetAllDisease(CancellationToken cancellationToken)
        {
            return await _service.GetAllDiseases(cancellationToken);
        }

        [HttpGet("{id}")]
        public async Task<DiseaseResponse> GetDiseaseById(Guid id, CancellationToken cancellationToken)
        {
            return await _service.GetDiseaseById(id, cancellationToken);
        }

    }
}
