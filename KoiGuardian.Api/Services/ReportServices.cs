using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.DataAccess;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using KoiGuardian.Models.Enums;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace KoiGuardian.Api.Services
{
    public interface IReportServices
    {
        Task<ReportResponse> CreateReport (CreateReportRequest request, CancellationToken cancellationToken);
        Task<ReportResponse> UpdateReport(UpdateReportRequest request, CancellationToken cancellationToken);
        Task<IList<Report>> GetAllReportAsync(CancellationToken cancellationToken);
        Task<ReportDetailResponse> GetReportByIDAsync(Guid id,CancellationToken cancellationToken);
        Task<List<ReportDetailResponse>> GetReportByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    }
    public class ReportServices : IReportServices
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Report> _reportRepository;
        private readonly IRepository<DataAccess.Db.Transaction> _transactionRepository;
        private readonly IRepository<Wallet> _walletRepository;

        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public ReportServices(
           IRepository<Order> orderRepository,
           IRepository<Report> reportRepository,
           IRepository<Wallet> walletRepository,
           IRepository<DataAccess.Db.Transaction> transactionRepository,
           IUnitOfWork<KoiGuardianDbContext> unitOfWork)
        {
            _orderRepository = orderRepository;
            _reportRepository = reportRepository;
            _walletRepository = walletRepository;
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

        public async Task<IList<Report>> GetAllReportAsync(CancellationToken cancellationToken)
        {
            return await _reportRepository.GetQueryable().ToListAsync(cancellationToken);
        }

        public async Task<ReportDetailResponse> GetReportByIDAsync(Guid id, CancellationToken cancellationToken)
        {
            var report = await _reportRepository.GetAsync(
                u => u.ReportId == id,
                include: u => u.Include(u => u.Order)
                                .ThenInclude(o => o.OrderDetail)
                                .Include(u => u.Order.Shop)
                                .Include(u => u.Order.User)
            );

            if (report == null) return new ReportDetailResponse();

            AddressDto addressDto;
            try
            {
                addressDto = !string.IsNullOrEmpty(report.Order?.Address)
                    ? JsonSerializer.Deserialize<AddressDto>(report.Order.Address)
                    : new AddressDto { ProvinceName = "No address info" };
            }
            catch (JsonException)
            {
                addressDto = new AddressDto { ProvinceName = "Invalid address" };
            }

            return new ReportDetailResponse
            {
                ReportId = report.ReportId,
                OrderId = report.Order?.OrderId ?? Guid.Empty,
                CreatedDate = report.CreatedDate,
                Reason = report.Reason ?? "No reason provided",
                image = report.image ?? "No image",
                status = report.status ?? "Unknown status",
                order = new OrderDetailResponse
                {
                    OrderId = report.Order?.OrderId ?? Guid.Empty,
                    ShopName = report.Order?.Shop?.ShopName ?? "Unknown Shop",
                    CustomerName = report.Order?.User?.UserName ?? "Unknown Customer",
                    CustomerAddress = addressDto,
                    CustomerPhoneNumber = report.Order?.User?.PhoneNumber ?? "No phone number",
                    ShipFee = report.Order?.ShipFee ?? "No Fee",
                    oder_code = report.Order?.oder_code ?? "Unknown Code",
                    Status = report.Order?.Status ?? "Unknown Status",
                    ShipType = report.Order?.ShipType ?? "Unknown Ship Type",
                    Note = report.Order?.Note ?? "No note",
                    Details = report.Order?.OrderDetail?.Select(d => new OrderDetailDto
                    {
                        ProductId = d.ProductId,
                        Quantity = d.Quantity
                    }).ToList() ?? new List<OrderDetailDto>()
                }
            };
        }

        public async Task<List<ReportDetailResponse>> GetReportByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            var reports = await _reportRepository.FindAsync(
                u => u.Order.UserId == userId.ToString(),
                include: u => u.Include(u => u.Order)
                                .ThenInclude(o => o.OrderDetail)
                                .Include(u => u.Order.Shop)
                                .Include(u => u.Order.User),
                orderBy: u => u.OrderByDescending(u => u.CreatedDate)
            );

            return reports.Select(report =>
            {
                AddressDto addressDto;
                try
                {
                    addressDto = !string.IsNullOrEmpty(report.Order?.Address)
                        ? JsonSerializer.Deserialize<AddressDto>(report.Order.Address)
                        : new AddressDto { ProvinceName = "No address info" };
                }
                catch (JsonException)
                {
                    addressDto = new AddressDto { ProvinceName = "Invalid address" };
                }

                return new ReportDetailResponse
                {
                    ReportId = report.ReportId,
                    OrderId = report.Order?.OrderId ?? Guid.Empty,
                    CreatedDate = report.CreatedDate,
                    Reason = report.Reason ?? "No reason provided",
                    image = report.image ?? "No image",
                    status = report.status ?? "Unknown status",
                    order = new OrderDetailResponse
                    {
                        OrderId = report.Order?.OrderId ?? Guid.Empty,
                        ShopName = report.Order?.Shop?.ShopName ?? "Unknown Shop",
                        CustomerName = report.Order?.User?.UserName ?? "Unknown Customer",
                        CustomerAddress = addressDto,
                        CustomerPhoneNumber = report.Order?.User?.PhoneNumber ?? "No phone number",
                        ShipFee = report.Order?.ShipFee,
                        oder_code = report.Order?.oder_code,
                        Status = report.Order?.Status,
                        ShipType = report.Order?.ShipType,
                        Note = report.Order?.Note,
                        Details = report.Order?.OrderDetail?.Select(d => new OrderDetailDto
                        {
                            ProductId = d.ProductId,
                            Quantity = d.Quantity
                        }).ToList() ?? new List<OrderDetailDto>()
                    }
                };
            }).ToList();
        }

        public async Task<ReportResponse> UpdateReport(UpdateReportRequest request, CancellationToken cancellationToken)
        {
            ReportResponse reportResponse = new ReportResponse();
            var report = await _reportRepository.GetAsync(x => x.ReportId == request.ReportId, CancellationToken.None);
            var transaction = await _transactionRepository.GetAsync(x => x.DocNo.Equals(report.OrderId), CancellationToken.None);
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
                    transaction.TransactionType = TransactionType.Pending.ToString();
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
                    var wallet = await _walletRepository.GetAsync(x => x.UserId.Equals(transaction.UserId),cancellationToken);
                    var order = await _orderRepository.GetAsync(x => x.OrderId.Equals(transaction.DocNo),cancellationToken);
                    if(order != null && wallet != null)
                    {
                        order.Status = OrderStatus.Fail.ToString();
                        order.UpdatedDate = DateTime.UtcNow;
                        transaction.TransactionType = TransactionType.Cancel.ToString() ;

                        var RefundInfo = new RefundInfo
                        {
                            Amount = (decimal)order.Total,
                            Date = DateTime.UtcNow,
                            Description = $"Hoàn Tiền cho hóa đơn {order.OrderId}"
                        };
                        var jsonOptions = new JsonSerializerOptions
                        {
                            WriteIndented = true,  // Tạo định dạng xuống dòng và thụt đầu dòng
                            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // Hỗ trợ ký tự tiếng Việt
                        };
                        string refundJson = JsonSerializer.Serialize(RefundInfo, jsonOptions);

                        transaction.Refund = refundJson;
                        wallet.Amount += order.Total;
                        _transactionRepository.Update(transaction);
                        _orderRepository.Update(order);
                        _walletRepository.Update(wallet);
                    }
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
