using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Enums;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace KoiGuardian.Api.Services
{
    public interface ITransactionService
    {
        Task<ShopTransactionResponseDto> GetTransactionbyShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
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
         IRepository<AccountPackage> ACrepository,
         IRepository<Shop> shopRepository,
        IRepository<Wallet> walletRepository,
         IUnitOfWork<KoiGuardianDbContext> uow
         ) : ITransactionService
    {
        private const decimal FeePercentage = 0.03m; // 3% phí trên mỗi đơn hàng

        public async Task<ShopTransactionResponseDto> GetTransactionbyShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            // Retrieve orders for the shop
            var orders = await orderRepository.FindAsync(x => x.ShopId == shopId, cancellationToken);
            var orderIds = orders.Select(o => o.OrderId).ToList();

            // Retrieve transactions for the orders
            var transactions = await transactionRepository.FindAsync(
                x => orderIds.Contains(x.DocNo),
                cancellationToken
            );

            // Retrieve the shop's wallet
            var shop = (await shopRepository.FindAsync(s => s.ShopId == shopId, cancellationToken)).FirstOrDefault();
            if (shop == null)
            {
                return new ShopTransactionResponseDto
                {
                    Transactions = new List<TransactionDto>(),
                    ShopBalance = 0m,
                };
            }

            var wallet = (await walletRepository.FindAsync(w => w.UserId == shop.UserId, cancellationToken)).FirstOrDefault();
            var shopBalance = wallet != null ? (decimal)wallet.Amount : 0m;

            // Map transactions to TransactionDto and sort by TransactionDate
            var transactionList = transactions.Select(t =>
            {
                var order = orders.FirstOrDefault(o => o.OrderId == t.DocNo);
                return new TransactionDto
                {
                    TransactionId = t.TransactionId,
                    TransactionDate = t.TransactionDate,
                    TransactionType = t.TransactionType,
                    VnPayTransactionId = t.VnPayTransactionid,
                    Amount = order != null ? (decimal)order.Total : 0m,
                    Payment = !string.IsNullOrEmpty(t.Payment)
                        ? JsonSerializer.Deserialize<PaymentInfo>(t.Payment)
                        : null,
                    Refund = !string.IsNullOrEmpty(t.Refund)
                        ? JsonSerializer.Deserialize<RefundInfo>(t.Refund)
                        : null
                };
            })
            .OrderByDescending(t => t.TransactionDate) // Sort by TransactionDate in descending order
            .ToList();

            // Return response DTO
            return new ShopTransactionResponseDto
            {
                ShopBalance = shopBalance,
                Transactions = transactionList,
                
            };
        }

        public async Task<List<TransactionPackageDto>> GetTransactionPackagebyOwnerIdAsync(Guid ownerId)
        {
            // Lấy tất cả transactions của owner
            var transactions = await transactionRepository.FindAsync(
                x => x.UserId.Equals(ownerId.ToString()),
                CancellationToken.None
            );

            if (!transactions.Any())
                return new List<TransactionPackageDto>();

            // Lấy tất cả AccountPackageId từ DocNo trong transaction
            var accountPackageIds = transactions.Select(t => t.DocNo).ToList();

            // Lấy danh sách AccountPackage theo Id
            var accountPackages = await ACrepository.FindAsync(
                x => accountPackageIds.Contains(x.AccountPackageid),
                CancellationToken.None
            );

            // Lấy tất cả PackageId từ các accountPackages
            var packageIds = accountPackages.Select(ap => ap.PackageId).Distinct().ToList();

            // Lấy danh sách Package
            var packages = await packageRepository.FindAsync(
                x => packageIds.Contains(x.PackageId),
                CancellationToken.None
            );

            // Map dữ liệu
            var result = transactions
             .Where(t => accountPackages.Any(ap => ap.AccountPackageid == t.DocNo))
             .Select(t =>
             {
                 var accountPackage = accountPackages.FirstOrDefault(ap => ap.AccountPackageid == t.DocNo);
                 var package = packages.FirstOrDefault(p => p.PackageId == accountPackage?.PackageId);

                 return new TransactionPackageDto
                 {
                     TransactionId = t.TransactionId,
                     TransactionDate = t.TransactionDate,
                     TransactionType = t.TransactionType,
                     Description = t.VnPayTransactionid,
                     ExpiryDate = accountPackage.PurchaseDate.AddDays(package.Peiod),
                     PakageName = package?.PackageTitle ?? string.Empty,
                     Amount = (decimal)t.Amount
                 };
             })
             .OrderByDescending(x => x.TransactionDate)
             .ToList();

            return result;
        }


        public async Task<List<TransactionDto>> GetTransactionDespositbyOwnerIdAsync(Guid ownerId)
        {
            var orders = await orderRepository.FindAsync(x => true, CancellationToken.None);
            var orderIds = orders.Select(o => o.OrderId).ToList();

            var ACpackages = await ACrepository.FindAsync(x => true, CancellationToken.None);
            var ACpackageIds = ACpackages.Select(p => p.AccountPackageid).ToList();

            var transactions = await transactionRepository.FindAsync(
                x => x.UserId == ownerId.ToString()
                    && !orderIds.Contains(x.DocNo)
                    && !ACpackageIds.Contains(x.DocNo),
                CancellationToken.None
            );

            var result = transactions.Select(t => new TransactionDto
            {
                TransactionId = t.TransactionId,
                TransactionDate = t.TransactionDate,
                TransactionType = t.TransactionType,
                Amount = (decimal)t.Amount,
                VnPayTransactionId = t.VnPayTransactionid
                
            })
                .OrderByDescending(x => x.TransactionDate)
                .ToList();

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
                    DocNo = t.DocNo,
                    VnPayTransactionId = t.VnPayTransactionid,
                    Amount = order != null ? (decimal)order.Total : 0m, // Lấy Total từ Order
                    Payment = !string.IsNullOrEmpty(t.Payment)
                ? JsonSerializer.Deserialize<PaymentInfo>(t.Payment)
                : null,

                    Refund = !string.IsNullOrEmpty(t.Refund)
                ? JsonSerializer.Deserialize<RefundInfo>(t.Refund)
                : null,
                };
            })
                 .OrderByDescending(x => x.TransactionDate)
                .ToList();

            return result;
        }

       public async Task<RevenueSummaryDto> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var result = new RevenueSummaryDto();

            // Chuyển đổi startDate và endDate thành UTC nếu có giá trị
            var startDateUtc = startDate.HasValue ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var endDateUtc = endDate.HasValue ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc) : (DateTime?)null;

            var transactions = await transactionRepository.FindAsync(
                x => (x.TransactionType.ToLower().Equals(TransactionType.Success.ToString().ToLower())) &&
                     (!startDateUtc.HasValue || x.TransactionDate >= startDateUtc.Value) &&
                     (!endDateUtc.HasValue || x.TransactionDate <= endDateUtc.Value),
                CancellationToken.None
            );

            var orders = await orderRepository.FindAsync(
                x => x.Status.ToLower().Equals(OrderStatus.Complete.ToString().ToLower()),
                CancellationToken.None
            );
            var orderIds = orders.Select(o => o.OrderId).ToList();

            var ACpackages = await ACrepository.FindAsync(
                x => true,
                CancellationToken.None
            );
            var ACpackageIds = ACpackages.Select(p => p.AccountPackageid).ToList();

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

            var packageTransactions = transactions.Where(t => ACpackageIds.Contains(t.DocNo)).ToList();
            result.PackageTransactionCount = packageTransactions.Count;
            foreach (var transaction in packageTransactions)
            {
                var ACpackage = ACpackages.FirstOrDefault(p => p.AccountPackageid == transaction.DocNo);
                if (ACpackage != null)
                {
                    result.PackageRevenue += (decimal)transaction.Amount;
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
                     (x.TransactionType.ToLower().Equals(TransactionType.Success.ToString().ToLower()))&&
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
            result.FailedOrders = orders.Count(o => o.Status.ToLower() == OrderStatus.Return.ToString().ToLower() 
                                            || o.Status.ToLower() == OrderStatus.Cancel.ToString().ToLower()
                                            || o.Status.ToLower() == OrderStatus.Fail.ToString().ToLower());
            result.PendingOrders = orders.Count(o => o.Status.ToLower() != OrderStatus.Return.ToString().ToLower() 
                                            && o.Status.ToLower() != OrderStatus.Cancel.ToString().ToLower()
                                            && o.Status.ToLower() != OrderStatus.Complete.ToString().ToLower()
                                            && o.Status.ToLower() != OrderStatus.Fail.ToString().ToLower());
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
            result.FailedOrders = orders.Count(o => o.Status.ToLower() == OrderStatus.Return.ToString().ToLower()
                                            || o.Status.ToLower() == OrderStatus.Cancel.ToString().ToLower()
                                            || o.Status.ToLower() == OrderStatus.Fail.ToString().ToLower());
            result.PendingOrders = orders.Count(o => o.Status.ToLower() != OrderStatus.Return.ToString().ToLower()
                                            && o.Status.ToLower() != OrderStatus.Cancel.ToString().ToLower()
                                            && o.Status.ToLower() != OrderStatus.Complete.ToString().ToLower()
                                            && o.Status.ToLower() != OrderStatus.Fail.ToString().ToLower());
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