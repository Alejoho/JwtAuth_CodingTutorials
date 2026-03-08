namespace WebApiAuthentication.DTOs;

public class LoginResponseDto
{
    public required string JwtToken { get; set; }
    public DateTime Expiration { get; set; }
    public required string RefreshToken { get; set; }
}
