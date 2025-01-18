using Microsoft.AspNetCore.Http;

namespace KoiGuardian.Models.Request;

public class RegistrationRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;


    public string Password { get; set; } = string.Empty;

    public string? Role { get; set; } = string.Empty;
    public string? Gender { get; set; } = string.Empty;
    public string? Address { get; set; } = string.Empty;
    public IFormFile? Avatar { get; set; } 
}