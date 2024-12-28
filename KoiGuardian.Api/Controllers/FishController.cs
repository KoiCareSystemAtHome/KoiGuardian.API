using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class FishController : ControllerBase
{
    private readonly IFishService _fishService;

    public FishController(IFishService fishService)
    {
        _fishService = fishService;
    }

    // POST: api/Fish/Create
    [HttpPost("Create")]
    public async Task<IActionResult> CreateFishAsync([FromBody] FishRequest fishRequest, CancellationToken cancellationToken)
    {
        if (fishRequest == null)
        {
            return BadRequest("Fish data is required.");
        }

        FishResponse response = await _fishService.CreateFishAsync(fishRequest, cancellationToken);

        if (response.Status == "201")
        {
            
            return Created($"/api/Fish/{fishRequest.KoiID}", response);

           
        }

        return Conflict(response);
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<Fish>> GetFishByIdAsync(int id, CancellationToken cancellationToken)
    {
        var fish = await _fishService.GetFishByIdAsync(id, cancellationToken);
        if (fish == null)
        {
            return NotFound($"Fish with ID {id} was not found.");
        }
        return Ok(fish);
    }


    [HttpPut("Update")]
    public async Task<IActionResult> UpdateFishAsync([FromBody] FishRequest fishRequest, CancellationToken cancellationToken)
    {
        if (fishRequest == null)
        {
            return BadRequest("Fish data is required.");
        }

        FishResponse response = await _fishService.UpdateFishAsync(fishRequest, cancellationToken);

        if (response.Status == "200")
        {
            return Ok(response);
        }

        return NotFound(response);
    }
}