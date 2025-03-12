namespace KoiGuardian.Models.Response;
public class LoginResponse
{
    public UserDto User { get; set; } = new UserDto();
    public string Token { get; set; } = string.Empty;
}
public class UserDto
{
    public string ID { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? PackageID { get; set; } 
    public string Status { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
}
