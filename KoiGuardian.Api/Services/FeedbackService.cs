using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.DataAccess;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using System;
using System.Threading;
using System.Threading.Tasks;

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
            var product = await _productRepository.GetAsync(
                x => x.ProductId == feedbackRequest.ProductId,
                cancellationToken: cancellationToken
            );

            if (product == null)
            {
                response.Status = "404";
                response.Message = "Product not found.";
                return response;
            }

            // Get the user by matching MemberId
            var user = await _userRepository.GetAsync(
                x => x.Id == feedbackRequest.MemberId, // Assuming x.Id is a string
                cancellationToken: cancellationToken
            );

            if (user == null)
            {
                response.Status = "404";
                response.Message = "User not found.";
                return response;
            }

            // Create feedback
            var feedback = new Feedback
            {
                ProductId = feedbackRequest.ProductId,
                MemberId = feedbackRequest.MemberId,
                Rate = feedbackRequest.Rate,
                Content = feedbackRequest.Content
            };

            // Insert feedback into the repository
            _feedbackRepository.Insert(feedback);

            try
            {
                // Save changes
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
