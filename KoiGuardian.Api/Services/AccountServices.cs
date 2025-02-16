using AutoMapper;
using Azure;
using Azure.Core;
using KoiGuardian.Api.Constants;
using KoiGuardian.Api.Helper;
using KoiGuardian.Api.Utils;
using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    Task<string> UpdateAmount(string email, float amount);
    Task<string> UpdateAccountPackage(string email, Guid packageId);

    Task<string> ConfirmResetPassCode(string email, int code, string newPass);
}

public class AccountService
(UserManager<User> _userManager,
RoleManager<IdentityRole> _roleManager,
IJwtTokenGenerator _jwtTokenGenerator,
IRepository<User> userRepository,
IRepository<Member> memberRepository,
IRepository <Package> packageRepository,
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

    public Task<bool> ActivateAccount(string email, string code)
    {
        throw new NotImplementedException();
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
        var user = await userRepository.GetAsync(u => (u.UserName ?? string.Empty).Equals(username.ToLower()), cancellation);

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
        var roles = await _userManager.GetRolesAsync(user);

        var token = _jwtTokenGenerator.GenerateToken(user, roles);
        UserDto userDto = new()
        {
            Email = user.Email ?? string.Empty,
            ID = user.Id,
            Name = user.UserName ?? string.Empty,
            PackageID = user.PackageId,
            Avatar = mem.Avatar,
            Status = user.Status.ToString(),
            Gender = mem.Gender,
            Address = mem.Address,
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
            UserName = registrationRequestDto.Email,
            Email = registrationRequestDto.Email,
            NormalizedEmail = registrationRequestDto.Email.ToUpper(),
            Status = UserStatus.NotVerified,
            Code = SD.RandomCode(),
            CreatedDate = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow,
        };

        var avatar = await imageUpload.UploadImageAsync(baseUrl, "User", user.Id, registrationRequestDto.Avatar);

        try
        {
            var result = await _userManager.CreateAsync(user, registrationRequestDto.Password);

            if (result.Succeeded)
            {

                var assignRole = await AssingRole(user, ConstantValue.MemberRole, cancellationToken);

                var userToReturn = await userRepository.GetQueryable().AsNoTracking().Where(u => u.Email == registrationRequestDto.Email)
                    .FirstOrDefaultAsync();


                memberRepository.Insert(new Member()
                {
                    MemberId = Guid.NewGuid().ToString(),
                    UserId = userToReturn.Id,
                    Address = registrationRequestDto.Address,
                    Avatar = avatar, 
                    Gender = registrationRequestDto.Gender, 
                    
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

    public async Task<string> ActivateAccount(string username, int code)
    {
        var user = await userRepository.GetAsync(u => (u.UserName ?? string.Empty).Equals(username.ToLower()), CancellationToken.None);

        if (user == null)
        {
            return "User is not found";
        }

        if (user.Code != code)
        {
            return "Code is invalid";
        }

        if (user.ValidUntil < DateTime.Now)
        {
            return "Code is expired, please resend it";
        }

        string sendMail = SendMail.SendEmail(user.Email ?? "", "KoiGuardin thank you", EmailTemplate.VerifySuccess(user.UserName ?? ""), "");

        user.Status = UserStatus.Active;
        userRepository.Update(user);
        await uow.SaveChangesAsync(CancellationToken.None);

        return "";
    }

    public async Task<bool> ResendCode(string email)
    {
        var user = await userRepository.GetAsync(u => (u.UserName ?? string.Empty).Equals(email.ToLower()), CancellationToken.None);

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
        var user = await userRepository.GetAsync(u => (u.UserName ?? string.Empty).Equals(email.ToLower()), CancellationToken.None);

        if (user == null)
        {
            return "User not found";
        }

        user.Code = SD.RandomCode();
        user.ValidUntil = DateTime.Now.AddMinutes(5);

        string sendMail = SendMail.SendEmail(user.Email ?? "", "Code for register", EmailTemplate.Register(user.Code), "");
        if (sendMail != "")
        {
            return "Email invalid!";
        }

        userRepository.Update(user);
        await uow.SaveChangesAsync(CancellationToken.None);

        return "";
    }

    public async Task<string> ChangePassword(string email, string oldPass, string newPass)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null && oldPass != null && newPass != null)
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

        return "";
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
                    if (DateTime.Now > (user.ValidUntil ?? DateTime.Now).AddMinutes(10))
                    {
                        return "Expired code! ";
                    }

                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var result = await _userManager.ResetPasswordAsync(user, token, newPass);
                }

            }
            return "";
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

        var roles = await _userManager.GetRolesAsync(user);

        var token = _jwtTokenGenerator.GenerateToken(user, roles);
        UserDto userDto = new()
        {
            Email = user.Email ?? string.Empty,
            ID = user.Id,
            Name = user.UserName ?? string.Empty,
            PackageID = user.PackageId,
            Avatar = mem.Avatar,
            Status = user.Status.ToString(),
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
        var user = await userRepository.GetAsync(u => (u.Email ?? string.Empty).Equals(request.Email.ToLower()), CancellationToken.None);
        var member = await memberRepository.GetAsync(u => (u.UserId ?? string.Empty).Equals(user.Id), CancellationToken.None);
        if (user == null || user.Status != UserStatus.Active)
        {
            return "Account is not valid!";
        }

        var address = JsonSerializer.Deserialize<List<string>>(member.Address);
        if (address != null)
        {
            address.Add(request.Address);
        }
        user.UserName = request.Name;
        member.Address = JsonSerializer.Serialize(address);
        member.Avatar = await imageUpload.UploadImageAsync(baseUrl, "User", user.Id, request.Avatar);
        member.Gender = request.Gender;
        user.UserReminder = request.UserReminder;

        await uow.SaveChangesAsync();

        return string.Empty;
    }

    public async Task<string> UpdateAmount(string email, float amount)
    {
        //var user = await userRepository.GetAsync(u => (u.Email ?? string.Empty).Equals(email.ToLower()), CancellationToken.None);

        //if (user == null || user.Status != UserStatus.Active)
        //{
        //    return "Account is not valid!";
        //}
        //user.Amount += amount;
        //userRepository.Update(user);
        //await uow.SaveChangesAsync();
        return string.Empty;
    }

    public async Task<string> UpdateAccountPackage(string email, Guid packageId)
    {
        //var user = await userRepository.GetAsync(u => (u.Email ?? string.Empty).Equals(email.ToLower()), CancellationToken.None);

        ////check xem là user có đang còn là thành viên ko
        //var checkUserExistPackage = await ACrepository.GetAsync(u => u.PackageId.Equals(user.PackageId), CancellationToken.None);

        //var package = await packageRepository.GetAsync(u => u.PackageId.Equals(packageId), CancellationToken.None);


        //if (user == null || user.Status != UserStatus.Active)
        //{
        //    return "Account is not valid!";
        //}

        //if(checkUserExistPackage.PurchaseDate.AddMonths(1) > DateTime.UtcNow)
        //{
        //    return "Your Account still on date";
        //}

        //if((decimal)user.Amount < package.PackagePrice)
        //{
        //    return "Your Balance is not enough";
        //}

        //if(package == null || package.EndDate < DateTime.UtcNow || package.StartDate > DateTime.UtcNow)
        //{
        //    return "Package is not valid!";
        //}

        //user.Amount -= (float)package.PackagePrice;
        //user.PackageId = packageId;
        
        //ACrepository.Insert(new AccountPackage
        //{
        //    AccountPackageid = Guid.NewGuid(),
        //    AccountId = user.Id,
        //    PackageId = packageId,
        //    PurchaseDate = DateTime.UtcNow,
        //});
        //userRepository.Update(user);
        //await uow.SaveChangesAsync();
        return "Success";
    }
}

