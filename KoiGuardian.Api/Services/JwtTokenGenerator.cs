using KoiGuardian.DataAccess.Db;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using KoiGuardian.Models.Commons;

namespace KoiGuardian.Api.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, IEnumerable<string> roles);
}

public class JwtTokenGenerator(IOptions<JwtOptions> _jwtOptions) : IJwtTokenGenerator
{
    public string GenerateToken(User User, IEnumerable<string> roles)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var key = Encoding.ASCII.GetBytes(_jwtOptions.Value.Secret);

        var claimList = new List<Claim>
            {
                new (JwtRegisteredClaimNames.Email,User.Email!),
                new (JwtRegisteredClaimNames.Sub,User.Id),
                new (JwtRegisteredClaimNames.Name,User.UserName!)
            };
        claimList.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));


        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = _jwtOptions.Value.Audience,
            Issuer = _jwtOptions.Value.Issuer,
            Subject = new ClaimsIdentity(claimList),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}