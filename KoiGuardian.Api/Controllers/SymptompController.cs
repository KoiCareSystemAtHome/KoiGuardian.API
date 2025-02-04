﻿using KoiGuardian.Api.Services;
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
        public async Task<List<Symptom>> GetListComm( string? type)
        {
            return await service.GetByType(type);
        }

        [HttpGet("predict")]
        public async Task<DiseaseTypePredictResponse> DiseaseTypePredict(List<DiseaseTypePredictRequest> symptoms      )
        {
            return await service.DiseaseTypePredict(symptoms);
        }

        [HttpGet("examination")]
        public async Task<FinalDiseaseTypePredictResponse> Examination(List<DiseaseTypePredictRequest> symptoms)
        {
            return await service.Examination(symptoms);
        }
    }
}
