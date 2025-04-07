﻿using AutoMapper;
using Azure;
using Azure.Core;
using KoiGuardian.Api.Constants;
using KoiGuardian.Api.Helper;
using KoiGuardian.Api.Utils;
using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Enums;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using static KoiGuardian.Models.Enums.CommonEnums;

namespace KoiGuardian.Api.Services;

public interface IAccountServices
{
    Task<LoginResponse> Login(string username, string password, CancellationToken cancellation);
    Task<LoginResponse> Login(string email, CancellationToken cancellation);

    Task<bool> AssingRole(User user, string roleName, CancellationToken cancellation);

    Task<string> Register(string baseurl, RegistrationRequestDto model, CancellationToken cancellationToken);

    Task<AccountDashboardResponse> AccountDashboard(DateTime? dateTime, DateTime? endDate);

    Task<List<UserDto>> Filter(AccountFilterRequest request);

    Task<string> ActivateAccount(string email, int code);

    Task<bool> ResendCode(string email);

    Task<string> ForgotPassword(string email);

    Task<string> ChangePassword(string email, string oldPass, string newPass);
    Task<string> UpdateProfile(string baseUrl, UpdateProfileRequest request);
    Task<string> UpdateAmount(string email, float amount, string VnPayTransactionId);
    Task<(string Message, bool IsSuccess, DateTime? ExpirationDate)> UpdateAccountPackage(string email, Guid packageId, bool forceRenew = false);
    Task<string> UpdateAccountOrder(string email, List<Guid> orderIds);
    Task<string> ProcessPendingTransactions(DateTime inputDate);
    Task<List<User>> GetMember();

    Task<string> ConfirmResetPassCode(string email, int code, string newPass);

    Task<WalletResponse> GetWalletByOwnerId(Guid OwnerId);
}

public class AccountService
(UserManager<User> _userManager,
RoleManager<IdentityRole> _roleManager,
IJwtTokenGenerator _jwtTokenGenerator,
IRepository<User> userRepository,
IRepository<Member> memberRepository,
IRepository<Transaction> tranctionRepository,
IRepository<Shop> shopRepository,
IRepository <Package> packageRepository,
IRepository <Wallet> walletRepository,
IRepository<Order> orderRepository,
IRepository<AccountPackage> ACrepository,
IMapper mapper,
IUnitOfWork<KoiGuardianDbContext> uow,
IImageUploadService imageUpload
)
: IAccountServices
{
    public async Task<AccountDashboardResponse> AccountDashboard(DateTime? startDate, DateTime? endDate)
    {
        startDate = startDate ?? DateTime.UtcNow.AddMonths(-1);
        endDate = endDate ?? DateTime.UtcNow;
        return await userRepository.GetQueryable()
            .Where(u => u.CreatedDate < endDate && u.CreatedDate > startDate)
            .GroupBy(u => 1)
            .Select(u => new AccountDashboardResponse()
            {
                TotalUser = u.Count(),
                //TotalShop = TODO
                TotalActiveUser = u.Count(u => u.Status == UserStatus.Active),
                TotalInactiveUser = u.Count(u => u.Status == UserStatus.InActived),
                TotaNotVerifiedlUser = u.Count(u => u.Status == UserStatus.NotVerified),
                TotalBannedUser = u.Count(u => u.Status == UserStatus.Banned),
                PreniumUser = u.Count(u => u.PackageId != null), // TODO and package is valid
                UnPreniumUser = u.Count(u => u.PackageId == null)
            }).FirstAsync();
    }


    public async Task<bool> AssingRole(User user, string roleName, CancellationToken cancellation)
    {
        try
        {

            if (!_roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
            };

            await _userManager.AddToRoleAsync(user, roleName);
            return true;

        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<List<UserDto>> Filter(AccountFilterRequest request)
    {
        var result = userRepository.GetQueryable();

        if (!string.IsNullOrEmpty(request.UserName))
        {
            result = result.Where(u => u.UserName == request.UserName);
        }

        if (request.Status != null)
        {
            result = result.Where(u => u.Status == request.Status);
        }

        if (request.IsUsingPackage != null)
        {
            result = result.Where(u => u.PackageId != null);
        }

        if (request.PackageID != null)
        {
            result = result.Where(u => u.PackageId == request.PackageID);
        }

        return mapper.Map<List<UserDto>>(await result.ToListAsync());
    }

    public async Task<LoginResponse> Login(string username, string password, CancellationToken cancellation)
    {
        var user = await userRepository.GetAsync(u => (u.Email ?? string.Empty).Equals(username.ToLower()), cancellation);

        bool isvalid = await _userManager.CheckPasswordAsync(user ?? new User(), password);

        if (user == null || isvalid == false || user.Status != UserStatus.Active)
        {
            return new()
            {
                User = new(),
                Token = string.Empty
            };
        }
        var mem = await memberRepository.GetAsync(u => (u.UserId ?? string.Empty).Equals(user.Id.ToLower()), cancellation);
        var shop = await shopRepository.GetAsync(u => (u.UserId ?? string.Empty).Equals(user.Id.ToLower()), cancellation);

        var roles = await _userManager.GetRolesAsync(user);

        var token = _jwtTokenGenerator.GenerateToken(user, roles);
        UserDto userDto = new()
        {
            Email = user.Email ?? string.Empty,
            ID = user.Id,
            Name = user.UserName ?? string.Empty,
            PackageID = user.PackageId,
            Avatar = mem?.Avatar ?? "",
            Status = user.Status.ToString(),
            Gender = mem?.Gender ?? "",
            Address = mem?.Address ?? "",
            ShopId = shop?.ShopId.ToString() ?? "",
            GHNid = shop?.GHNId ??""
        };

        LoginResponse loginResponse = new()
        {
            User = userDto,
            Token = token
        };

        return loginResponse;
    }

    public async Task<string> Register(string baseUrl, RegistrationRequestDto registrationRequestDto, CancellationToken cancellationToken)
    {

        var user = new User()
        {
            UserName = registrationRequestDto.UserName,
            Email = registrationRequestDto.Email,
            NormalizedEmail = registrationRequestDto.Email.ToUpper(),
            Status = UserStatus.NotVerified,
            Code = SD.RandomCode(),
            CreatedDate = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddMinutes(5),
        };

        var exist = await userRepository.GetAsync(x => x.Email.Equals(registrationRequestDto.Email), CancellationToken.None);
        if(exist != null)
        {
            return "Email has been used";
        }

        var avatar = await imageUpload.UploadImageAsync("User", user.Id, registrationRequestDto.Avatar);

        try
        {
            var result = await _userManager.CreateAsync(user, registrationRequestDto.Password);

            if (result.Succeeded)
            {

                var assignRole = await AssingRole(user, registrationRequestDto.Role, cancellationToken);

                var userToReturn = await userRepository.GetQueryable().AsNoTracking().Where(u => u.Email == registrationRequestDto.Email)
                    .FirstOrDefaultAsync();

                if(registrationRequestDto.Role.ToLower() == "member")
                {
                    memberRepository.Insert(new Member()
                    {
                        MemberId = Guid.NewGuid().ToString(),
                        UserId = userToReturn.Id,
                        Address = registrationRequestDto.Address,
                        Avatar = avatar, 
                        Gender = registrationRequestDto.Gender,
                    
                    });
                }

                if (registrationRequestDto.Role.ToLower() == "shop")
                {
                    shopRepository.Insert(new Shop()
                    {
                       ShopId = Guid.NewGuid(),
                       ShopName = registrationRequestDto.Name,
                       ShopRate = 0,
                       ShopDescription = " ",
                       ShopAddress = registrationRequestDto.Address,
                       IsActivate = false,
                       BizLicences = " ",
                       UserId = userToReturn.Id

                    });
                }

                walletRepository.Insert( new Wallet()
                {
                    WalletId = Guid.NewGuid(),
                    UserId = userToReturn.Id,
                    PurchaseDate = DateTime.UtcNow,
                    Amount = 0,
                    Status = WalletStatus.avai.ToString()
                });

                await uow.SaveChangesAsync();

                UserDto userDto = new UserDto()
                {
                    Email = userToReturn?.Email ?? string.Empty,
                    ID = userToReturn?.Id ?? string.Empty,
                    Avatar = userToReturn?.PhoneNumber ?? string.Empty,
                    Name = userToReturn?.UserName ?? string.Empty,

                };

                string sendMail = SendMail.SendEmail(user.Email, "Code for register", EmailTemplate.Register(user.Code), "");
                if (sendMail != "")
                {
                    return "Please check you email to verify your account for using others further features.";
                }

            }
            return result.Errors?.FirstOrDefault()?.Description ?? string.Empty;

        }
        catch (Exception e)
        {
            return e.Message;
        };
    }

    public async Task<string> ActivateAccount(string email, int code)
    {
        var user = await userRepository.GetAsync(u => (u.Email ?? string.Empty).Equals(email.ToLower()), CancellationToken.None);

        if (user == null)
        {
            return "User is not found";
        }

        if (user.Code != code)
        {
            return "Code is invalid";
        }

        if (user.ValidUntil < DateTime.UtcNow)
        {
            return "Code is expired, please resend it";
        }

        string sendMail = SendMail.SendEmail(user.Email ?? "", "KoiGuardin thank you", EmailTemplate.VerifySuccess(user.UserName ?? ""), "");

        user.Status = UserStatus.Active;
        userRepository.Update(user);
        await uow.SaveChangesAsync(CancellationToken.None);

        return "success";
    }

    public async Task<bool> ResendCode(string email)
    {
        var user = await userRepository.GetAsync(u => (u.Email ?? string.Empty).Equals(email.ToLower()), CancellationToken.None);

        if (user == null)
        {
            return false;
        }

        user.Code = SD.RandomCode();
        user.ValidUntil = DateTime.UtcNow.AddMinutes(5);

        string sendMail = SendMail.SendEmail(user.Email ?? "", "Code for register", EmailTemplate.Register(user.Code), "");
        if (sendMail != "")
        {
            return false;
        }

        userRepository.Update(user);
        await uow.SaveChangesAsync(CancellationToken.None);

        return true;
    }

    public async Task<string> ForgotPassword(string email)
    {
        var user = await userRepository.GetAsync(u => (u.Email ?? string.Empty).Equals(email.ToLower()), CancellationToken.None);

        if (user == null)
        {
            return "User not found";
        }

        user.Code = SD.RandomCode();
        user.ValidUntil = DateTime.UtcNow.AddMinutes(5);

        string sendMail = SendMail.SendEmail(user.Email ?? "", "Code for register", EmailTemplate.Register(user.Code), "");
        if (sendMail != "")
        {
            return "Email invalid!";
        }

        userRepository.Update(user);
        await uow.SaveChangesAsync(CancellationToken.None);

        return "success";
    }

    public async Task<string> ChangePassword(string email, string oldPass, string newPass)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null && oldPass == null && newPass == null)
        {
            return "Invalid data";
        }

        try
        {
            var result = await _userManager.ChangePasswordAsync(user, oldPass, newPass);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        return "success";
    }

    public async Task<string> ConfirmResetPassCode(string email, int code, string newPass)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && user.Email != null)
            {
                if (user.Status == UserStatus.InActived)
                {
                    return "Account inactive";
                }
                if (user.Code == code)
                {
                    if (DateTime.UtcNow > (user.ValidUntil ?? DateTime.UtcNow).AddMinutes(10))
                    {
                        return "Expired code! ";
                    }

                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var result = await _userManager.ResetPasswordAsync(user, token, newPass);
                    if (result.Errors.Count() > 0)
                        return JsonSerializer.Serialize(result.Errors);
                }

            }
            
            return "success";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public async Task<LoginResponse> Login(string email, CancellationToken cancellation)
    {
        var user = await userRepository.GetAsync(u => (u.Email ?? string.Empty).Equals(email.ToLower()), cancellation);

        if (user == null || user.Status != UserStatus.Active)
        {
            return new()
            {
                User = new(),
                Token = string.Empty
            };
        }
        var mem = await memberRepository.GetAsync(u => (u.UserId ?? string.Empty).Equals(user.Id.ToLower()), cancellation);
        var shop = await shopRepository.GetAsync(u => (u.UserId ?? string.Empty).Equals(user.Id.ToLower()), cancellation);

        var roles = await _userManager.GetRolesAsync(user);

        var token = _jwtTokenGenerator.GenerateToken(user, roles);
        UserDto userDto = new()
        {
            Email = user.Email ?? string.Empty,
            ID = user.Id,
            Name = user.UserName ?? string.Empty,
            PackageID = user.PackageId,
            Avatar = mem.Avatar, // Ưu tiên avatar của shop, nếu không có thì lấy avatar của member
            Status = user.Status.ToString(),
            GHNid = shop?.GHNId ?? "",
            ShopId = shop.ShopId.ToString(),
        };

        LoginResponse loginResponse = new()
        {
            User = userDto,
            Token = token
        };

        return loginResponse;
    }

    public async Task<string> UpdateProfile(string baseUrl, UpdateProfileRequest request)
    {
        var user = await userRepository.GetAsync(
            u => u.Email != null && u.Email.ToLower().Equals(request.Email.ToLower()),
            CancellationToken.None);

        if (user == null || user.Status != UserStatus.Active)
        {
            return "Account is not valid!";
        }

        var member = await memberRepository.GetAsync(
            m => m.UserId != null && m.UserId.Equals(user.Id),
            CancellationToken.None);

        if (member == null)
        {
            var shop = await shopRepository.GetAsync(
            m => m.UserId != null && m.UserId.Equals(user.Id),
            CancellationToken.None);
            if (shop == null)
            { return "Accoutn not found"; }    
            shop.ShopDescription = request.ShopDescription ?? "";
            shop.BizLicences = request.BizLicense ?? "";
            shop.ShopAddress = request.address != null ? JsonSerializer.Serialize(request.address) : string.Empty;
            shopRepository.Update(shop);
        }else
        {
            // Update member details
            member.Gender = request.Gender;
            member.Address = request.address != null ? JsonSerializer.Serialize(request.address) : string.Empty;
            // Upload avatar if provided
            if (request.Avatar != null)
            {
                member.Avatar = request.Avatar;
            }
            memberRepository.Update(member);

        }

        // Update user details
        user.UserName = request.Name;
        user.UserReminder = TimeOnly.FromDateTime( request.UserReminder);


        await uow.SaveChangesAsync();

        return "Profile updated successfully!";
    }


    public async Task<string> UpdateAmount(string email, float amount, string VnPayTransactionId)
    {
        var user = await userRepository.GetAsync(u => (u.Email ?? string.Empty).Equals(email.ToLower()), CancellationToken.None);

        var wallet = await walletRepository.GetAsync(u => u.UserId.Equals(user.Id), CancellationToken.None);
        if (user == null || user.Status != UserStatus.Active)
        {
           return "Account is not valid!";
        }
        if (wallet == null)
        {
            return "Wallet is not valid!";
        }
        wallet.Amount += amount;
        walletRepository.Update(wallet);
        tranctionRepository.Insert(new Transaction
        {
            TransactionId = Guid.NewGuid(),
            TransactionDate = DateTime.UtcNow,
            TransactionType = TransactionType.Success.ToString(),
            Amount = amount,
            VnPayTransactionid = VnPayTransactionId,
            UserId = user.Id,
            DocNo = Guid.Parse(user.Id)
        });
        
        await uow.SaveChangesAsync();
        return string.Empty;
    }

    public async Task<(string Message, bool IsSuccess, DateTime? ExpirationDate)> UpdateAccountPackage(string email, Guid packageId, bool forceRenew = false)
    {
        var user = await userRepository.GetAsync(u => (u.Email ?? string.Empty).Equals(email.ToLower()), CancellationToken.None);

        if (user == null || user.Status != UserStatus.Active)
        {
            return ("Account is not valid!", false, null);
        }

        var wallet = await walletRepository.GetAsync(u => u.UserId.Equals(user.Id), CancellationToken.None);
        if (wallet == null)
        {
            return ("Wallet is not valid!", false, null);
        }

        // Kiểm tra gói hiện tại của user
        var currentPackage = await ACrepository.GetAsync(u => u.AccountId.Equals(user.Id), CancellationToken.None);
        var package = await packageRepository.GetAsync(u => u.PackageId.Equals(packageId), CancellationToken.None);

        if (currentPackage != null)
        {
            var expirationDate = currentPackage.PurchaseDate.AddDays(package.Peiod);
            if (expirationDate > DateTime.UtcNow && !forceRenew)
            {
                return ($"Your account still has an active package until {expirationDate:dd/MM/yyyy HH:mm:ss UTC}. Do you want to renew anyway?", false, expirationDate);
            }
        }

        if (package == null || package.EndDate < DateTime.UtcNow || package.StartDate > DateTime.UtcNow)
        {
            return ("Package is not valid!", false, null);
        }

        if ((decimal)wallet.Amount < package.PackagePrice)
        {
            return ("Your Balance is not enough", false, null);
        }

        // Chỉ tiến hành mua nếu forceRenew = true hoặc không có gói cũ còn hạn
        DateTime newPurchaseDate = DateTime.UtcNow;

        // Trừ tiền và cập nhật gói
        wallet.Amount -= (float)package.PackagePrice;
        user.PackageId = packageId;

        // Thêm gói mới
        ACrepository.Insert(new AccountPackage
        {
            AccountPackageid = Guid.NewGuid(),
            AccountId = user.Id,
            PackageId = packageId,
            PurchaseDate = newPurchaseDate,
        });

        // Ghi nhận giao dịch
        tranctionRepository.Insert(new Transaction
        {
            TransactionId = Guid.NewGuid(),
            TransactionDate = DateTime.UtcNow,
            TransactionType = TransactionType.Success.ToString(),
            VnPayTransactionid = "Pay By Wallet",
            UserId = user.Id,
            Amount = (float)package.PackagePrice,
            DocNo = package.PackageId
        });

        // Cập nhật lại ví
        walletRepository.Update(wallet);

        await uow.SaveChangesAsync();
        return ("Success", true, null);
    }

    public async Task<string> UpdateAccountOrder(string email, List<Guid> orderIds)
    {
        var user = await userRepository.GetAsync(u => (u.Email ?? string.Empty).Equals(email.ToLower()), CancellationToken.None);

        if (user == null || user.Status != UserStatus.Active)
        {
            return "Account is not valid!";
        }

        var wallet = await walletRepository.GetAsync(u => u.UserId.Equals(user.Id), CancellationToken.None);
        if (wallet == null)
        {
            return "Wallet is not valid!";
        }

        var orders = await orderRepository
           .FindAsync(o => orderIds.Contains(o.OrderId) && o.UserId == user.Id);
          

        if (!orders.Any()) return "No valid orders found";
        float totalAmount = orders.Sum(o => o.Total);

        if (wallet.Amount < totalAmount)
        {
            return "Your Balance is not enough";
        }
        wallet.Amount -= totalAmount;
        foreach (var order in orders)
        {
            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                TransactionDate = DateTime.UtcNow,
                TransactionType = TransactionType.Pending.ToString(),
                VnPayTransactionid = "Order Paid By Wallet", 
                UserId = user.Id,
                DocNo = order.OrderId, 
            };

           orderRepository.Update(order);
           tranctionRepository.Insert(transaction);

        }
        walletRepository.Update(wallet);
        await uow.SaveChangesAsync();
        return "Success";

    }

    public async Task<string> ProcessPendingTransactions(DateTime inputDate)
    {
        // Lấy danh sách giao dịch Pending
        var pendingTransactions = await tranctionRepository
            .FindAsync(t => t.TransactionType.ToLower() == TransactionType.Pending.ToString().ToLower());

        if (!pendingTransactions.Any()) return "No pending transactions found!";

        var orderIds = pendingTransactions.Select(t => t.DocNo).ToList();

        // Lọc các đơn hàng có ShipType là Complete, OrderStatus là Complete và UpdateDate quá 3 ngày
        var eligibleOrders = await orderRepository.FindAsync(o =>
            orderIds.Contains(o.OrderId) &&
            // o.ShipType.ToLower() == ShipType.Complete.ToString().ToLower() &&
            o.Status.ToLower() == OrderStatus.Complete.ToString().ToLower() &&
            (inputDate - o.UpdatedDate).TotalDays >= 3
        );

        if (!eligibleOrders.Any()) return "No eligible orders found!";

        // Nhóm các đơn hàng theo ShopId và tính tổng tiền
        var shopTransactions = eligibleOrders
            .GroupBy(o => o.ShopId)
            .Select(group => new
            {
                ShopId = group.Key,
                TotalAmount = group.Sum(o => o.Total)*0.97,
            })
            .ToList();

        var shopIds = shopTransactions.Select(s => s.ShopId).ToList();
        var shops = await shopRepository.FindAsync(s => shopIds.Contains(s.ShopId));
        var shopWallets = await walletRepository.FindAsync(w => shops.Select(s => s.UserId).Contains(w.UserId));

        foreach (var shopTran in shopTransactions)
        {
            var shop = shops.FirstOrDefault(s => s.ShopId == shopTran.ShopId);
            if (shop == null) continue;

            var shopWallet = shopWallets.FirstOrDefault(w => w.UserId == shop.UserId);
            if (shopWallet == null) return "Shop Wallet is not valid!";

            // Cộng tiền vào ví của shop
            shopWallet.Amount += (float)shopTran.TotalAmount;
            walletRepository.Update(shopWallet);
        }

        // Cập nhật trạng thái giao dịch thành Success nếu đơn hàng hợp lệ
        foreach (var transaction in pendingTransactions)
        {
            if (eligibleOrders.Any(o => o.OrderId == transaction.DocNo))
            {
                transaction.TransactionType = TransactionType.Success.ToString();
                tranctionRepository.Update(transaction);
            }
        }

        await uow.SaveChangesAsync();
        return "Wallet update successful!";
    }

    public async Task<List<User>> GetMember()
    {
        var usersInRole = await _userManager.GetUsersInRoleAsync("Member");

        var result = await userRepository.FindAsync(
            u => usersInRole.Select(u => u.Id).Contains(u.Id),
            include: u => u.Include(u => u.Member)
                           .Include(u => u.Wallet)
        );

        return result.Select(u => new User
        {
            Id = u.Id,
            UserName = u.UserName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            NormalizedEmail = u.NormalizedEmail,
            PackageId = u.PackageId,
            Status= u.Status,
            CreatedDate = u.CreatedDate,
            AccessFailedCount = u.AccessFailedCount,
            ConcurrencyStamp = u.ConcurrencyStamp,
            EmailConfirmed = u.EmailConfirmed,
            ValidUntil  = u.ValidUntil,
            UserReminder = u.UserReminder,
            PasswordHash = u.PasswordHash,
            Wallet = u.Wallet != null ? new Wallet {
                WalletId= u.Wallet.WalletId, 
                Amount = u.Wallet.Amount, 
                UserId = u.Wallet.UserId,
                Status = u.Wallet.Status.ToString(),
            } : null,
            Member = u.Member != null ? u.Member : null
        }).ToList();
    }

    public async Task<WalletResponse> GetWalletByOwnerId(Guid OwnerId)
    {
        var wallet = await walletRepository.GetAsync(x => x.UserId.Equals(OwnerId.ToString()), CancellationToken.None);
        if (wallet == null)
        {
            return new WalletResponse();
        }
        return new WalletResponse
        {
            WalletId = wallet.WalletId,
            Amount = wallet.Amount,
            UserId = wallet.UserId,
            Status = wallet.Status,
            PurchaseDate = wallet.PurchaseDate,
        };
    }
}

