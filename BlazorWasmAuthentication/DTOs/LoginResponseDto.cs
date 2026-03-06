namespace BlazorWasmAuthentication.DTOs;

public class LoginResponseDto
{
    public required string JwtToken { get; set; }
    public DateTime Expiration { get; set; }
}
