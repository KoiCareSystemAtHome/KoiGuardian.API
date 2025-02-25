﻿using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FishController(
        IFishService fishService,
        IKoiDiseaseService _services) : ControllerBase
    {
        [HttpGet]
        public async Task<List<Fish>> GetAllFishAsync([FromQuery] string? name = null, CancellationToken cancellationToken = default)
        {
            return await fishService.GetAllFishAsync(name,cancellationToken);
        }

        [HttpGet("get-by-owner")]
        public async Task<List<FishDto>> GetAllFishOwnerAsync([FromQuery] Guid owner , CancellationToken cancellationToken = default)
        {
            return await fishService.GetFishByOwnerId(owner, cancellationToken);
        }


        [HttpPost("create-fish")]
        public async Task<FishResponse> CreateFishAsync([FromBody] FishRequest fishRequest, CancellationToken cancellationToken)
        {
            return await fishService.CreateFishAsync(fishRequest, cancellationToken);
        }

        [HttpPut("update-fish")]
        public async Task<FishResponse> UpdateFishAsync([FromBody] FishRequest fishRequest, CancellationToken cancellationToken)
        {
            return await fishService.UpdateFishAsync(fishRequest, cancellationToken);
        }

        [HttpGet("{koiId}")]
        public async Task<FishResponse> GetFishByIdAsync(Guid koiId, CancellationToken cancellationToken)
        {
            var fish = await fishService.GetFishByIdAsync(koiId, cancellationToken);
            if (fish == null)
            {
                return new FishResponse
                {
                    Status = "404",
                    Message = $"Fish with ID {koiId} was not found."
                };
            }

            return new FishResponse
            {
                Status = "200",
                Message = "Fish retrieved successfully.",
               Fish = new FishDto()
               {
                   KoiID  = fish.KoiID,
                   Age = fish.Age,
                   Image = fish.Image,  
                   Name = fish.Name,
                   Price = fish.Price,
                   Sex = fish.Sex,
                   Pond = fish.Pond,
                   Variety = fish.Variety,
                   DiseaseTracking = await _services.GetDisease(koiId),
               }
            };
        }
    }
}
