using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.DataAccess;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using KoiGuardian.Models.Enums;
using System.Transactions;

namespace KoiGuardian.Api.Services
{
    public interface IReportServices
    {
        Task<ReportResponse> CreateReport (CreateReportRequest request, CancellationToken cancellationToken);
        Task<ReportResponse> UpdateReport(UpdateReportRequest request, CancellationToken cancellationToken);
    }
    public class ReportServices : IReportServices
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Report> _reportRepository;
        private readonly IRepository<DataAccess.Db.Transaction> _transactionRepository;

        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public ReportServices(
           IRepository<Order> orderRepository,
           IRepository<Report> reportRepository,
           IRepository<DataAccess.Db.Transaction> transactionRepository,
           IUnitOfWork<KoiGuardianDbContext> unitOfWork)
        {
            _orderRepository = orderRepository;
            _reportRepository = reportRepository;
            _transactionRepository = transactionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ReportResponse> CreateReport(CreateReportRequest request, CancellationToken cancellationToken)
        {
            ReportResponse reportResponse = new ReportResponse();
            var order = await _orderRepository.GetAsync(x => x.OrderId == request.OrderId,CancellationToken.None);
            if(order == null)
            {
                reportResponse.message = "Order not exist!!!";
                reportResponse.status = "400";
                return reportResponse;
            }
            var transaction = await _transactionRepository.GetAsync(x => x.DocNo.Equals(order.OrderId.ToString()),CancellationToken.None);
            if (transaction != null)
            {
             transaction.TransactionType = TransactionType.Report.ToString();
            _transactionRepository.Update(transaction);
            }

            _reportRepository.Insert(new Report
            {
                ReportId = Guid.NewGuid(),
                OrderId = request.OrderId,
                CreatedDate = DateTime.UtcNow,
                image = request.Image,
                Reason = request.Reason,
                status = ReportStatus.Pending.ToString(),
            });
           

            _unitOfWork.SaveChangesAsync();
            reportResponse.message = "Create Success";
            reportResponse.status = "200";
            return reportResponse;
        }

        public async Task<ReportResponse> UpdateReport(UpdateReportRequest request, CancellationToken cancellationToken)
        {
            ReportResponse reportResponse = new ReportResponse();
            var report = await _reportRepository.GetAsync(x => x.ReportId == request.ReportId, CancellationToken.None);
            var transaction = await _transactionRepository.GetAsync(x => x.DocNo.Equals(report.OrderId.ToString()), CancellationToken.None);
            if (report == null)
            {
                reportResponse.message = "Report not exist!!!";
                reportResponse.status = "400";
                return reportResponse;
            }

            /*if (transaction == null)
            {
                reportResponse.message = "Transaction not exist!!!";
                reportResponse.status = "400";
                return reportResponse;
            }*/

            if (ReportStatus.Reject.ToString().ToLower().Equals(request.statuz.ToLower()))
            {
                report.status = ReportStatus.Reject.ToString();
                if (transaction != null)
                {
                    transaction.TransactionType = TransactionType.Success.ToString();
                    _transactionRepository.Update(transaction);
                }
                
            }
            if (ReportStatus.Approve.ToString().ToLower().Equals(request.statuz.ToLower()))
            {
                report.status = ReportStatus.Approve.ToString();
               
                if (transaction != null)
                {
                    transaction.TransactionType = TransactionType.Cancel.ToString();
                    _transactionRepository.Update(transaction);
                }
            }

            _reportRepository.Update(report);
            
            _unitOfWork.SaveChangesAsync();
            reportResponse.message = "Update Success";
            reportResponse.status = "200";
            return reportResponse;
        }
    }
}
