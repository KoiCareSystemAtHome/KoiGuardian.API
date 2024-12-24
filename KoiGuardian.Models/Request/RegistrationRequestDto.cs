namespace KoiGuardian.Models.Request;

public class RegistrationRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Avatar { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? Role { get; set; } = string.Empty;
}