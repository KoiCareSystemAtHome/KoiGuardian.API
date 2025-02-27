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
    public class ReportController(
        IReportServices services
        ) : ControllerBase
    {
        [HttpPost("create-report")]
        public async Task<ReportResponse> CreateReportAsync([FromBody] CreateReportRequest Request, CancellationToken cancellationToken)
        {
            return await services.CreateReport(Request, cancellationToken);
        }

        [HttpPut("update-report")]
        public async Task<ReportResponse> UpdateReportAsync([FromBody] UpdateReportRequest Request, CancellationToken cancellationToken)
        {
            return await services.UpdateReport(Request, cancellationToken);
        }
    }
}
