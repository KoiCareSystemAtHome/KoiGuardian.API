using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Enums;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;

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
        Task<OrderStatusSummaryDto> GetOrderStatusSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<OrderStatusSummaryDto> GetOrderStatusSummaryByShopIdAsync(Guid shopId, DateTime? startDate = null, DateTime? endDate = null);
        Task<ProductSalesSummaryDto> GetProductSalesSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<ProductSalesSummaryDto> GetProductSalesSummaryByShopIdAsync(Guid shopId, DateTime? startDate = null, DateTime? endDate = null);
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

        public async Task<OrderStatusSummaryDto> GetOrderStatusSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var result = new OrderStatusSummaryDto();

            var startDateUtc = startDate.HasValue ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var endDateUtc = endDate.HasValue ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc) : (DateTime?)null;

            var orders = await orderRepository.FindAsync(
                predicate: o => (!startDateUtc.HasValue || o.CreatedDate >= startDateUtc.Value) &&
                                (!endDateUtc.HasValue || o.CreatedDate <= endDateUtc.Value),
                include: null, // Không cần Include vì chỉ dùng thông tin từ Order
                orderBy: o => o.OrderByDescending(o => o.CreatedDate)
            );

            result.SuccessfulOrders = orders.Count(o => o.Status.ToLower() == OrderStatus.Complete.ToString().ToLower());
            result.FailedOrders = orders.Count(o => o.Status.ToLower() == OrderStatus.Fail.ToString().ToLower());
            result.PendingOrders = orders.Count(o => o.Status.ToLower() == OrderStatus.Pending.ToString().ToLower());
            result.TotalOrders = orders.Count();

            return result;
        }

        public async Task<OrderStatusSummaryDto> GetOrderStatusSummaryByShopIdAsync(Guid shopId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var result = new OrderStatusSummaryDto();

            var startDateUtc = startDate.HasValue ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var endDateUtc = endDate.HasValue ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc) : (DateTime?)null;

            var orders = await orderRepository.FindAsync(
                predicate: o => o.ShopId == shopId &&
                                (!startDateUtc.HasValue || o.CreatedDate >= startDateUtc.Value) &&
                                (!endDateUtc.HasValue || o.CreatedDate <= endDateUtc.Value),
                include: null, // Không cần Include vì chỉ dùng thông tin từ Order
                orderBy: o => o.OrderByDescending(o => o.CreatedDate)
            );

            result.SuccessfulOrders = orders.Count(o => o.Status.ToLower() == OrderStatus.Complete.ToString().ToLower());
            result.FailedOrders = orders.Count(o => o.Status.ToLower() == OrderStatus.Fail.ToString().ToLower());
            result.PendingOrders = orders.Count(o => o.Status.ToLower() == OrderStatus.Pending.ToString().ToLower());
            result.TotalOrders = orders.Count();

            return result;
        }

        public async Task<ProductSalesSummaryDto> GetProductSalesSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var result = new ProductSalesSummaryDto();

            var startDateUtc = startDate.HasValue ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var endDateUtc = endDate.HasValue ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc) : (DateTime?)null;

            var orders = await orderRepository.FindAsync(
                predicate: o => o.Status.ToLower() == OrderStatus.Complete.ToString().ToLower() &&
                                (!startDateUtc.HasValue || o.CreatedDate >= startDateUtc.Value) &&
                                (!endDateUtc.HasValue || o.CreatedDate <= endDateUtc.Value),
                include: o => o.Include(o => o.OrderDetail).ThenInclude(od => od.Product),
                orderBy: o => o.OrderByDescending(o => o.CreatedDate)
            );

            // Tải dữ liệu vào bộ nhớ trước khi GroupBy để tránh lỗi CS1662
            var orderDetails = orders.SelectMany(o => o.OrderDetail).ToList();

            var monthlySales = orderDetails
                .GroupBy(od => new
                {
                    Month = od.Order.CreatedDate.Month,
                    Year = od.Order.CreatedDate.Year,
                    Type = od.Product.Type
                })
                .Select(g => new ProductSalesByMonthDto
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    FoodCount = g.Where(x => (int)x.Product.Type == 0).Sum(x => x.Quantity), // 0 = Food
                    ProductCount = g.Where(x => (int)x.Product.Type == 1).Sum(x => x.Quantity), // 1 = Product
                    MedicineCount = g.Where(x => (int)x.Product.Type == 2).Sum(x => x.Quantity), // 2 = Medicine
                    TotalCount = g.Sum(x => x.Quantity)
                }).ToList();

            result.MonthlySales = monthlySales;
            result.TotalFood = monthlySales.Sum(m => m.FoodCount);
            result.TotalProducts = monthlySales.Sum(m => m.ProductCount);
            result.TotalMedicines = monthlySales.Sum(m => m.MedicineCount);
            result.TotalItemsSold = monthlySales.Sum(m => m.TotalCount);

            return result;
        }

        public async Task<ProductSalesSummaryDto> GetProductSalesSummaryByShopIdAsync(Guid shopId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var result = new ProductSalesSummaryDto();

            var startDateUtc = startDate.HasValue ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var endDateUtc = endDate.HasValue ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc) : (DateTime?)null;

            var orders = await orderRepository.FindAsync(
                predicate: o => o.ShopId == shopId &&
                                o.Status.ToLower() == OrderStatus.Complete.ToString().ToLower() &&
                                (!startDateUtc.HasValue || o.CreatedDate >= startDateUtc.Value) &&
                                (!endDateUtc.HasValue || o.CreatedDate <= endDateUtc.Value),
                include: o => o.Include(o => o.OrderDetail).ThenInclude(od => od.Product),
                orderBy: o => o.OrderByDescending(o => o.CreatedDate)
            );

            // Tải dữ liệu vào bộ nhớ trước khi GroupBy để tránh lỗi CS1662
            var orderDetails = orders.SelectMany(o => o.OrderDetail).ToList();

            var monthlySales = orderDetails
                .GroupBy(od => new
                {
                    Month = od.Order.CreatedDate.Month,
                    Year = od.Order.CreatedDate.Year,
                    Type = od.Product.Type
                })
                .Select(g => new ProductSalesByMonthDto
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    FoodCount = g.Where(x => (int)x.Product.Type == 0).Sum(x => x.Quantity), // 0 = Food
                    ProductCount = g.Where(x => (int)x.Product.Type == 1).Sum(x => x.Quantity), // 1 = Product
                    MedicineCount = g.Where(x => (int)x.Product.Type == 2).Sum(x => x.Quantity), // 2 = Medicine
                    TotalCount = g.Sum(x => x.Quantity)
                }).ToList();

            result.MonthlySales = monthlySales;
            result.TotalFood = monthlySales.Sum(m => m.FoodCount);
            result.TotalProducts = monthlySales.Sum(m => m.ProductCount);
            result.TotalMedicines = monthlySales.Sum(m => m.MedicineCount);
            result.TotalItemsSold = monthlySales.Sum(m => m.TotalCount);

            return result;
        }


    }
}