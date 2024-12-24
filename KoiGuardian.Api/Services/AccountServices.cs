using AutoMapper;
using KoiGuardian.Api.Constants;
using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;
using static KoiGuardian.Models.Enums.CommonEnums;

namespace KoiGuardian.Api.Services;

public interface IAuthServices
{
    Task<LoginResponse> Login(string username, string password, CancellationToken cancellation);

    Task<bool> AssingRole(User user, string roleName, CancellationToken cancellation);

    Task<string> Register(RegistrationRequestDto model, CancellationToken cancellationToken);

    Task<AccountDashboardResponse> AccountDashboard(DateTime? dateTime, DateTime? endDate);

    Task<List<UserDto>> Filter (AccountFilterRequest request);
}

public class AuthService
    (UserManager<User> _useManager,
    RoleManager<IdentityRole> _roleManager,
    IJwtTokenGenerator _jwtTokenGenerator,
    IRepository<User> userRepository,
    IMapper mapper
    )
    : IAuthServices
{
    public async Task<AccountDashboardResponse> AccountDashboard(DateTime? startDate ,DateTime? endDate)
    {
        startDate = startDate ?? DateTime.UtcNow.AddMonths(-1);
        endDate = endDate ?? DateTime.UtcNow;
        return await userRepository.GetQueryable()
            .Where( u =>u.CreatedDate < endDate && u.CreatedDate > startDate)
            .GroupBy(u => 1)
            .Select(u => new AccountDashboardResponse()
            {
                TotalUser = u.Count(),
                //TotalShop = TODO
                TotalActiveUser = u.Count( u => u.Status == UserStatus.Active),
                TotalInactiveUser = u.Count( u => u.Status == UserStatus.InActived),
                TotaNotVerifiedlUser = u.Count( u => u.Status == UserStatus.NotVerified),
                TotalBannedUser = u.Count(u => u.Status == UserStatus.Banned),
                PreniumUser = u.Count(u => u.PackageId != string.Empty), // TODO and package is valid
                UnPreniumUser = u.Count( u => u.PackageId == string.Empty)
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

            await _useManager.AddToRoleAsync(user, roleName);
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
            result = result.Where(u => u.Status == request.Status );
        }

        if (request.IsUsingPackage != null)
        {
            result = result.Where(u => !string.IsNullOrEmpty(u.PackageId));
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

        bool isvalid = await _useManager.CheckPasswordAsync(user ?? new User(), password);

        if (user == null || isvalid == false)
        {
            return new()
            {
                User = new(),
                Token = string.Empty
            };
        }
        var roles = await _useManager.GetRolesAsync(user);

        var token = _jwtTokenGenerator.GenerateToken(user, roles);
        UserDto userDto = new()
        {
            Email = user.Email ?? string.Empty,
            ID = user.Id,
            Name = user.UserName ?? string.Empty,
            PackageID = user.PackageId,
            Avatar = user.Avatar,
            Status = user.Status.ToString(),
        };

        LoginResponse loginResponse = new()
        {
            User = userDto,
            Token = token
        };

        return loginResponse;
    }

    public async Task<string> Register(RegistrationRequestDto registrationRequestDto, CancellationToken cancellationToken)
    {

        var user = new User()
        {
            UserName = registrationRequestDto.Email,
            Email = registrationRequestDto.Email,
            NormalizedEmail = registrationRequestDto.Email.ToUpper(),
            Status = UserStatus.NotVerified,
            Avatar = registrationRequestDto.Avatar,
            Code = 0
        };

        try
        {
            var result = await _useManager.CreateAsync(user, registrationRequestDto.Password);

            if (result.Succeeded)
            {
                var assignRole = await AssingRole(user, ConstantValue.MemberRole, cancellationToken);

                var userToReturn = await userRepository.GetAsync(u => u.Email == registrationRequestDto.Email,
                    cancellationToken);
                UserDto userDto = new UserDto()
                {
                    Email = userToReturn?.Email ?? string.Empty,
                    ID = userToReturn?.Id ?? string.Empty,
                    Avatar = userToReturn?.PhoneNumber ?? string.Empty,
                    Name = userToReturn?.UserName ?? string.Empty
                };
                return "Please check you email to verify your account for using others further features.";
            }
            else
            {
                return result.Errors?.FirstOrDefault()?.Description ?? string.Empty;
            }
        }
        catch (Exception e)
        {
            return e.Message;
        };
    }
}