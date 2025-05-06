using AutoMapper;
using KoiGuardian.Api.Utils;
using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Enums;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static KoiGuardian.Models.Enums.CommonEnums;

namespace KoiGuardian.Api.Services
{
    public interface IWalletWithdrawService
    {
        Task<(string Message, bool IsSuccess)> CreateWalletWithdraw(string userId, float amount, CancellationToken cancellationToken);
        Task<(string Message, bool IsSuccess)> UpdateWalletWithdraw(Guid withdrawId, string status, CancellationToken cancellationToken);
        Task<List<WalletWithdrawResponse>> GetWalletWithdrawByUserId(string userId, CancellationToken cancellationToken);
        Task<List<WalletWithdrawResponse>> GetWalletWithdrawByShopId(Guid shopId, CancellationToken cancellationToken);
        Task<List<WalletWithdrawResponse>> GetAllWalletWithdraw(CancellationToken cancellationToken);
    }

    public class WalletWithdrawService : IWalletWithdrawService
    {
        private readonly IRepository<WalletWithdraw> _walletWithdrawRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Shop> _shopRepository;
        private readonly IRepository<Wallet> _walletRepository;
        private readonly IRepository<Transaction> _transactionRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _uow;
        private readonly IMapper _mapper;

        public WalletWithdrawService(
            IRepository<WalletWithdraw> walletWithdrawRepository,
            IRepository<User> userRepository,
            IRepository<Shop> shopRepository,
            IRepository<Wallet> walletRepository,
            IRepository<Transaction> transactionRepository,
            IUnitOfWork<KoiGuardianDbContext> uow)
        {
            _walletWithdrawRepository = walletWithdrawRepository;
            _userRepository = userRepository;
            _shopRepository = shopRepository;
            _walletRepository = walletRepository;
            _transactionRepository = transactionRepository;
            _uow = uow;
           
        }

        public async Task<(string Message, bool IsSuccess)> CreateWalletWithdraw(string userId, float amount, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userRepository.GetAsync(u => u.Id.Equals(userId), cancellationToken);
                if (user == null || user.Status != UserStatus.Active)
                {
                    return ("User is not valid or inactive!", false);
                }

                var wallet = await _walletRepository.GetAsync(w => w.UserId.Equals(userId), cancellationToken);
                if (wallet == null)
                {
                    return ("Wallet not found!", false);
                }

                if ((wallet.Amount * 0.7) < amount || amount <= 0)
                {
                    return ($"Sô tiền rút ra không được nhiều hơn {wallet.Amount*0.7}", true);
                }

                var walletWithdraw = new WalletWithdraw
                {
                    AccountPackageId = Guid.NewGuid(),
                    UserId = userId,
                    PackageId = Guid.Empty, // Assuming PackageId is required; adjust if not needed
                    Code = SD.RandomCode(), // Assuming SD.RandomCode() generates a random code as in AccountService
                    Status = WalletWithdrawEnums.Pending.ToString(),
                    CreateDate = DateTime.UtcNow.AddHours(7),
                    PurchaseDate = DateTime.MinValue.ToUniversalTime(),
                    Money = amount
                };

                // Deduct the amount from wallet
                _walletWithdrawRepository.Insert(walletWithdraw);
                _walletRepository.Update(wallet);

                await _uow.SaveChangesAsync(cancellationToken);
                return ("Tạo yêu cầu rút tiền thành công!", true);
            }
            catch (Exception ex)
            {
                return ($"Error creating wallet withdraw request: {ex.Message}", false);
            }
        }

        public async Task<(string Message, bool IsSuccess)> UpdateWalletWithdraw(Guid withdrawId, string status, CancellationToken cancellationToken)
        {
            try
            {
                var walletWithdraw = await _walletWithdrawRepository.GetAsync(w => w.AccountPackageId.Equals(withdrawId), cancellationToken);
                if (walletWithdraw == null)
                {
                    return ("Wallet withdraw request not found!", false);
                }

                // Validate status
                if (!Enum.TryParse<WalletWithdrawEnums>(status, true, out var walletStatus))
                {
                    return ("Invalid status provided!", false);
                }

                walletWithdraw.Status = status;
                walletWithdraw.PurchaseDate = DateTime.UtcNow.AddHours(7);
                _walletWithdrawRepository.Update(walletWithdraw);

                
                if (walletStatus == WalletWithdrawEnums.Approve )
                {
                    var wallet = await _walletRepository.GetAsync(w => w.UserId.Equals(walletWithdraw.UserId), cancellationToken);
                    if (wallet == null)
                    {
                        return ("Wallet not found for refund!", false);
                    }

                    wallet.Amount -= walletWithdraw.Money;
                    _walletRepository.Update(wallet);
                    _transactionRepository.Insert(new Transaction
                    {
                        TransactionId = Guid.NewGuid(),
                        TransactionDate = DateTime.UtcNow,
                        TransactionType = TransactionType.Success.ToString(),
                        VnPayTransactionid = $"Rút Tiền Thành Công",
                        UserId = walletWithdraw.UserId,
                        Amount = walletWithdraw.Money,
                        DocNo = walletWithdraw.AccountPackageId
                    });
                }

                await _uow.SaveChangesAsync(cancellationToken);
                return ("Wallet withdraw request updated successfully!", true);
            }
            catch (Exception ex)
            {
                return ($"Error updating wallet withdraw request: {ex.Message}", false);
            }
        }

        public async Task<List<WalletWithdrawResponse>> GetWalletWithdrawByUserId(string userId, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetAsync(u => u.Id.Equals(userId), cancellationToken);
            if (user == null)
            {
                return new List<WalletWithdrawResponse>();
            }

            var walletWithdraws = await _walletWithdrawRepository
                .GetQueryable()
                .Where(w => w.UserId.Equals(userId))
                .ToListAsync(cancellationToken);

            var response = walletWithdraws.Select(w => new WalletWithdrawResponse
            {
                Id = w.AccountPackageId,
                UserId = w.UserId,
                //Code = w.Code,
                Status = w.Status,
                CreateDate = w.CreateDate,
                PurchaseDate = w.PurchaseDate,
                Money = w.Money
            }).ToList();

            return response;
        }

        public async Task<List<WalletWithdrawResponse>> GetWalletWithdrawByShopId(Guid shopId, CancellationToken cancellationToken)
        {
            var shop = await _shopRepository.GetAsync(s => s.ShopId.Equals(shopId), cancellationToken);
            if (shop == null || string.IsNullOrEmpty(shop.UserId))
            {
                return new List<WalletWithdrawResponse>();
            }

            var walletWithdraws = await _walletWithdrawRepository
                .GetQueryable()
                .Where(w => w.UserId.Equals(shop.UserId))
                .ToListAsync(cancellationToken);

            var transactions = await _transactionRepository
                .GetQueryable()
                .Where(t => t.UserId.Equals(shop.UserId) && walletWithdraws.Select(w => w.AccountPackageId).Contains(t.DocNo))
                .ToListAsync(cancellationToken);

            var response = walletWithdraws.Select(w => new WalletWithdrawResponse
            {
                Id = w.AccountPackageId,
                UserId = w.UserId,
                Status = w.Status,
                CreateDate = w.CreateDate,
                PurchaseDate = w.PurchaseDate,
                Money = w.Money,
                Transactions = transactions
                    .Where(t => t.DocNo == w.AccountPackageId)
                    .Select(t => new TransactionDto
                    {
                        TransactionId = t.TransactionId,
                        TransactionDate = t.TransactionDate,
                        TransactionType = t.TransactionType,
                        VnPayTransactionId = t.VnPayTransactionid,
                        Amount = (decimal)t.Amount,
                        Payment = null,
                        Refund = null
                    }).ToList()
            }).ToList();

            return response;
        }

        public async Task<List<WalletWithdrawResponse>> GetAllWalletWithdraw(CancellationToken cancellationToken)
        {
            var walletWithdraws = await _walletWithdrawRepository
                .GetQueryable()
                .ToListAsync(cancellationToken);

            // Nếu không có dữ liệu, trả về danh sách rỗng
            if (walletWithdraws == null || !walletWithdraws.Any())
            {
                return new List<WalletWithdrawResponse>();
            }

            var response = walletWithdraws.Select(w => new WalletWithdrawResponse
            {
                Id = w.AccountPackageId,
                UserId = w.UserId,
                //Code = w.Code, // Đã bị comment trong hàm gốc, giữ nguyên
                Status = w.Status,
                CreateDate = w.CreateDate,
                PurchaseDate = w.PurchaseDate,
                Money = w.Money
            }).ToList();

            return response;
        }
    }

    // Response DTO for WalletWithdraw
    public class WalletWithdrawResponse
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string Status { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime PurchaseDate { get; set; }
        public float Money { get; set; }
        public List<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
    }
}