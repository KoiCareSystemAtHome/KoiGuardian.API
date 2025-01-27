using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
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
        public async Task<List<Symptom>> GetListComm(SymptomType? type)
        {
            return await service.GetByType(type);
        }

        [HttpGet("predict")]
        public async Task<List<Symptom>> DiseaseTypePredict(List<Symptom> symptoms      )
        {
            return await service.DiseaseTypePredict(symptoms);
        }
    }
}
