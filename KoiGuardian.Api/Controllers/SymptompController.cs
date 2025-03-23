using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SymptompController
        (ISymptomService service) : ControllerBase
    {
        [HttpGet("type")]
        public async Task<List<PredictSymptoms>> GetListComm( string? type)
        {
            return await service.GetByType(type);
        }

        [HttpPost("predict")]
        public async Task<DiseaseTypePredictResponse> DiseaseTypePredict(List<DiseaseTypePredictRequest> symptoms      )
        {
            return await service.DiseaseTypePredict(symptoms);
        }

        [HttpPost("examination")]
        public async Task<FinalDiseaseTypePredictResponse> Examination
            ([FromBody]List<DiseaseTypePredictRequest> symptoms)
        {
            return await service.Examination(symptoms);
        }
    }
}
