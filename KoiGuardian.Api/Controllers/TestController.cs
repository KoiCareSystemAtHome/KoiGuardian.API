using KoiGuardian.DataAccess.MongoDB;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController(IKoiMongoDb test) : ControllerBase
    {
        [HttpPost("aaaaaaaaaaaaaaaaaaa")]
        public async Task<object> CreateBlog([FromBody] BlogRequest createBlog, CancellationToken cancellationToken)
        {
            return await test.GetDataFromMongo();
        }
    }
}
