using KoiGuardian.Api.Services;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        // POST api/feedback/create-feedback
        [HttpPost("create-feedback")]
        public async Task<FeedbackResponse> CreateFeedback([FromBody] FeedbackRequest feedbackRequest, CancellationToken cancellationToken)
        {
            return await _feedbackService.CreateFeedbackAsync(feedbackRequest, cancellationToken);
        }
    }
}
