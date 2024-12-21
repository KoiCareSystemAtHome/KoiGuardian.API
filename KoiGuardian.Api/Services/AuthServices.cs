using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Identity;

namespace KoiGuardian.Api.Services;

public interface IAuthServices
{
    Task<LoginResponse> Login(string username, string password, CancellationToken cancellation);
    Task<bool> AssingRole(String email, string roleName, CancellationToken cancellation);
}

public class AuthService
    (UserManager<User> _useManager,
    RoleManager<IdentityRole> _roleManager,
    IJwtTokenGenerator _jwtTokenGenerator,
    IRepository<User> userRepository

    )
    : IAuthServices
{
    public async Task<bool> AssingRole(string email, string roleName, CancellationToken cancellation)
    {
        var user = await userRepository.GetAsync(u => (u.Email ?? string.Empty).Equals(email.ToLower()), cancellation);
        if (user != null)
        {
            if (!_roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
            };
            await _useManager.AddToRoleAsync(user, roleName);
            return true;

        }
        return false;
    }

    public async Task<LoginResponse> Login(string username, string password, CancellationToken cancellation)
    {
        var user = await userRepository.GetAsync(u => (u.UserName ?? string.Empty).Equals(username.ToLower()), cancellation);
        
        bool isvalid = await _useManager.CheckPasswordAsync(user ?? new User(), password);
        
        if (user == null || isvalid == false)
        {
            return new ()
            {
                User = new (),
                Token = string.Empty
            };
        }
        var roles = await _useManager.GetRolesAsync(user);

        var token = _jwtTokenGenerator.GenerateToken(user, roles);
        UserDto userDto = new ()
        {
            Email = user.Email ?? string.Empty,
            ID = user.Id,
            Name = user.UserName ?? string.Empty,
            PackageID = user.PackageId,
            Avatar = user.Avatar,
            IsActivate = user.IsActivate
        };

        LoginResponse loginResponse = new ()
        {
            User = userDto,
            Token = token
        };

        return loginResponse;
    }
}