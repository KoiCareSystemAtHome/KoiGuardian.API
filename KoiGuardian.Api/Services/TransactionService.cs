using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Response;

namespace KoiGuardian.Api.Services
{
    public interface ITransactionService
    {
        Task<List<TransactionDto>> GetTransactionbyShopIdAsync(Guid shopId);
        Task<List<TransactionPackageDto>> GetTransactionPackagebyOwnerIdAsync(Guid ownerId);
        Task<List<TransactionDto>> GetTransactionDespositbyOwnerIdAsync(Guid ownerId);
        Task<List<TransactionDto>> GetTransactionOrderbyOwnerIdAsync(Guid ownerId);
        Task<RevenueSummaryDto> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<RevenueSummaryDto> GetRevenueByShopIdAsync(Guid shopId, DateTime? startDate = null, DateTime? endDate = null);
    }

    public class TransactionService(
         IRepository<Order> orderRepository,
         IRepository<Transaction> transactionRepository,
         IRepository<Package> packageRepository,
         IUnitOfWork<KoiGuardianDbContext> uow
         ) : ITransactionService
    {
        private const decimal FeePercentage = 0.03m; // 3% phí trên mỗi đơn hàng

        public async Task<List<TransactionDto>> GetTransactionbyShopIdAsync(Guid shopId)
        {
            var orders = await orderRepository.FindAsync(x => x.ShopId == shopId, CancellationToken.None);
            var orderIds = orders.Select(o => o.OrderId).ToList();

            var transactions = await transactionRepository.FindAsync(
                x => orderIds.Contains(x.DocNo),
                CancellationToken.None
            );

            var result = transactions.Select(t =>
            {
                var order = orders.FirstOrDefault(o => o.OrderId == t.DocNo);
                return new TransactionDto
                {
                    TransactionId = t.TransactionId,
                    TransactionDate = t.TransactionDate,
                    TransactionType = t.TransactionType,
                    VnPayTransactionId = t.VnPayTransactionid,
                    Amount = order != null ? (decimal)order.Total : 0m // Lấy Total từ Order
                };
            }).ToList();

            return result;
        }

        public async Task<List<TransactionPackageDto>> GetTransactionPackagebyOwnerIdAsync(Guid ownerId)
        {
            var packages = await packageRepository.FindAsync(
                x => true, // Lấy tất cả packages
                CancellationToken.None
            );
            var packageIds = packages.Select(p => p.PackageId).ToList();

            var transactions = await transactionRepository.FindAsync(
                x => packageIds.Contains(x.DocNo) && x.UserId.Equals(ownerId.ToString()),
                CancellationToken.None
            );

            var result = transactions.Select(t =>
            {
                var package = packages.FirstOrDefault(p => p.PackageId == t.DocNo);
                return new TransactionPackageDto
                {
                    TransactionId = t.TransactionId,
                    TransactionDate = t.TransactionDate,
                    TransactionType = t.TransactionType,
                    Description = t.VnPayTransactionid,
                    ExpiryDate = package.EndDate.AddDays(package.Peiod),
                    PakageName = package.PackageTitle,
                    Amount = package != null ? package.PackagePrice : 0m // Lấy PackagePrice
                };
            }).ToList();

            return result;
        }

        public async Task<List<TransactionDto>> GetTransactionDespositbyOwnerIdAsync(Guid ownerId)
        {
            var orders = await orderRepository.FindAsync(x => true, CancellationToken.None);
            var orderIds = orders.Select(o => o.OrderId).ToList();

            var packages = await packageRepository.FindAsync(x => true, CancellationToken.None);
            var packageIds = packages.Select(p => p.PackageId).ToList();

            var transactions = await transactionRepository.FindAsync(
                x => x.UserId == ownerId.ToString()
                    && !orderIds.Contains(x.DocNo)
                    && !packageIds.Contains(x.DocNo),
                CancellationToken.None
            );

            var result = transactions.Select(t => new TransactionDto
            {
                TransactionId = t.TransactionId,
                TransactionDate = t.TransactionDate,
                TransactionType = t.TransactionType,
                Amount = (decimal)t.Amount,
                VnPayTransactionId = t.VnPayTransactionid
            }).ToList();

            return result;
        }

        public async Task<List<TransactionDto>> GetTransactionOrderbyOwnerIdAsync(Guid ownerId)
        {
            var orders = await orderRepository.FindAsync(
                x => x.UserId == ownerId.ToString(),
                CancellationToken.None
            );
            var orderIds = orders.Select(o => o.OrderId).ToList();

            var transactions = await transactionRepository.FindAsync(
                x => orderIds.Contains(x.DocNo),
                CancellationToken.None
            );

            var result = transactions.Select(t =>
            {
                var order = orders.FirstOrDefault(o => o.OrderId == t.DocNo);
                return new TransactionDto
                {
                    TransactionId = t.TransactionId,
                    TransactionDate = t.TransactionDate,
                    TransactionType = t.TransactionType,
                    VnPayTransactionId = t.VnPayTransactionid,
                    Amount = order != null ? (decimal)order.Total : 0m // Lấy Total từ Order
                };
            }).ToList();

            return result;
        }

       public async Task<RevenueSummaryDto> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var result = new RevenueSummaryDto();

            // Chuyển đổi startDate và endDate thành UTC nếu có giá trị
            var startDateUtc = startDate.HasValue ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var endDateUtc = endDate.HasValue ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc) : (DateTime?)null;

            var transactions = await transactionRepository.FindAsync(
                x => (!startDateUtc.HasValue || x.TransactionDate >= startDateUtc.Value) &&
                     (!endDateUtc.HasValue || x.TransactionDate <= endDateUtc.Value),
                CancellationToken.None
            );

            var orders = await orderRepository.FindAsync(
                x => x.Status.ToLower().Equals(OrderStatus.Complete.ToString().ToLower()),
                CancellationToken.None
            );
            var orderIds = orders.Select(o => o.OrderId).ToList();

            var packages = await packageRepository.FindAsync(
                x => true,
                CancellationToken.None
            );
            var packageIds = packages.Select(p => p.PackageId).ToList();

            var orderTransactions = transactions.Where(t => orderIds.Contains(t.DocNo)).ToList();
            result.OrderTransactionCount = orderTransactions.Count;
            foreach (var transaction in orderTransactions)
            {
                var order = orders.FirstOrDefault(o => o.OrderId == transaction.DocNo);
                if (order != null)
                {
                    var orderAmount = (decimal)order.Total;
                    var fee = orderAmount * FeePercentage;
                    result.OrderRevenue += fee;
                }
            }

            var packageTransactions = transactions.Where(t => packageIds.Contains(t.DocNo)).ToList();
            result.PackageTransactionCount = packageTransactions.Count;
            foreach (var transaction in packageTransactions)
            {
                var package = packages.FirstOrDefault(p => p.PackageId == transaction.DocNo);
                if (package != null)
                {
                    result.PackageRevenue += package.PackagePrice;
                }
            }

            result.TotalRevenue = result.OrderRevenue + result.PackageRevenue;

            return result;
        }

        public async Task<RevenueSummaryDto> GetRevenueByShopIdAsync(Guid shopId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var result = new RevenueSummaryDto();

            var orders = await orderRepository.FindAsync(
                x => x.ShopId == shopId && x.Status.ToLower().Equals(OrderStatus.Complete.ToString().ToLower()),
                CancellationToken.None
            );
            var orderIds = orders.Select(o => o.OrderId).ToList();

            // Chuyển đổi startDate và endDate thành UTC nếu có giá trị
            var startDateUtc = startDate.HasValue ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var endDateUtc = endDate.HasValue ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc) : (DateTime?)null;

            var transactions = await transactionRepository.FindAsync(
                x => orderIds.Contains(x.DocNo) &&
                     (!startDateUtc.HasValue || x.TransactionDate >= startDateUtc.Value) &&
                     (!endDateUtc.HasValue || x.TransactionDate <= endDateUtc.Value),
                CancellationToken.None
            );

            result.OrderTransactionCount = transactions.Count;
            foreach (var transaction in transactions)
            {
                var order = orders.FirstOrDefault(o => o.OrderId == transaction.DocNo);
                if (order != null)
                {
                    var orderAmount = (decimal)order.Total;
                    var fee = orderAmount * FeePercentage;
                    result.OrderRevenue += orderAmount - fee;
                }
            }

            result.PackageTransactionCount = 0;
            result.PackageRevenue = 0;

            result.TotalRevenue = result.OrderRevenue + result.PackageRevenue;

            return result;
        }
    }
}