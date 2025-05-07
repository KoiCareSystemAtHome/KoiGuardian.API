using KoiGuardian.Api.Constants;
using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Authorization;
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
        [HttpGet]
        public async Task<IList<Report>> GetAllReports(CancellationToken cancellationToken)
        {
            return await services.GetAllReportAsync(cancellationToken);
        }

        [HttpGet("get-by-report-id")]
        public async Task<ReportDetailResponse> GetReportsById(Guid id,CancellationToken cancellationToken)
        {
            return await services.GetReportByIDAsync(id,cancellationToken);
        }

        [HttpGet("get-by-user-id")]
        public async Task<List<ReportDetailResponse>> GetReportByUserIds(Guid id, CancellationToken cancellationToken)
        {
            return await services.GetReportByUserIdAsync(id, cancellationToken);
        }

        [HttpPost("create-report")]
        [Authorize]
        public async Task<ReportResponse> CreateReportAsync([FromBody] CreateReportRequest Request, CancellationToken cancellationToken)
        {
            return await services.CreateReport(Request, cancellationToken);
        }

        [HttpPut("update-report")]
        [Authorize]
        public async Task<ReportResponse> UpdateReportAsync([FromBody] UpdateReportRequest Request, CancellationToken cancellationToken)
        {
            return await services.UpdateReport(Request, cancellationToken);
        }
    }
}
