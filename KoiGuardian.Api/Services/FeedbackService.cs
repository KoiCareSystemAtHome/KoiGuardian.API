using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.DataAccess;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;

namespace KoiGuardian.Api.Services
{
    public interface IFeedbackService
    {
        Task<FeedbackResponse> CreateFeedbackAsync(FeedbackRequest feedbackRequest, CancellationToken cancellationToken);
    }

    public class FeedbackService : IFeedbackService
    {
        private readonly IRepository<Feedback> _feedbackRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public FeedbackService(
           IRepository<Feedback> feedbackRepository,
           IRepository<Product> productRepository,
           IRepository<User> userRepository,
           IUnitOfWork<KoiGuardianDbContext> unitOfWork,
           ICurrentUser currentUser)
        {
            _feedbackRepository = feedbackRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<FeedbackResponse> CreateFeedbackAsync(FeedbackRequest feedbackRequest, CancellationToken cancellationToken)
        {
            var response = new FeedbackResponse();

            // Get the product
            var product = await _productRepository.GetAsync(x => x.ProductId == feedbackRequest.ProductId, cancellationToken);
            if (product == null)
            {
                response.Status = "404";
                response.Message = "Product not found.";
                return response;
            }

            // Use the current user’s ID instead of feedbackRequest.MemberId
            var memberId = _currentUser.UserName();  // Assuming UserName() returns the member's ID. Adjust if needed.

            if (string.IsNullOrEmpty(memberId))
            {
                response.Status = "400";
                response.Message = "User not authenticated.";
                return response;
            }

            var feedback = new Feedback
            {
                
                ProductId = feedbackRequest.ProductId,
                MemberId = memberId,  
                Rate = feedbackRequest.Rate,
                Content = feedbackRequest.Content
            };

            _feedbackRepository.Insert(feedback);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                response.Status = "201";
                response.Message = "Feedback created successfully.";
            }
            catch (Exception ex)
            {
                response.Status = "500";
                response.Message = $"Error creating feedback: {ex.Message}";
            }

            return response;
        }

    }
}
