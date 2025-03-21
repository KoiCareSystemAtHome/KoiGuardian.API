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
        Task<List<TransactionDto>> GetTransactionPackagebyOwnerIdAsync(Guid ownerId);
        Task<List<TransactionDto>> GetTransactionDespositbyOwnerIdAsync(Guid ownerId);
        Task<List<TransactionDto>> GetTransactionOrderbyOwnerIdAsync(Guid ownerId);
    }

    public class TransactionService(
         IRepository<Order> orderRepository,
         IRepository<Transaction> transactionRepository,
         IRepository<Package> packageRepository,
         IUnitOfWork<KoiGuardianDbContext> uow
         ) : ITransactionService
    {
        public async Task<List<TransactionDto>> GetTransactionbyShopIdAsync(Guid shopId)
        {
            var orders = await orderRepository.FindAsync(x => x.ShopId == shopId, CancellationToken.None);
            var orderIds = orders.Select(o => o.OrderId).ToList();

            var transactions = await transactionRepository.FindAsync(
                x => orderIds.Contains(x.DocNo),
                CancellationToken.None
            );

            var result = transactions.Select(t => new TransactionDto
            {
                TransactionId = t.TransactionId,
                TransactionDate = t.TransactionDate,
                TransactionType = t.TransactionType,
                VnPayTransactionId = t.VnPayTransactionid
            }).ToList();

            return result;
        }

        public async Task<List<TransactionDto>> GetTransactionPackagebyOwnerIdAsync(Guid ownerId)
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

            var result = transactions.Select(t => new TransactionDto
            {
                TransactionId = t.TransactionId,
                TransactionDate = t.TransactionDate,
                TransactionType = t.TransactionType,
                VnPayTransactionId = t.VnPayTransactionid
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

            var result = transactions.Select(t => new TransactionDto
            {
                TransactionId = t.TransactionId,
                TransactionDate = t.TransactionDate,
                TransactionType = t.TransactionType,
                VnPayTransactionId = t.VnPayTransactionid
            }).ToList();

            return result;
        }
    }
}