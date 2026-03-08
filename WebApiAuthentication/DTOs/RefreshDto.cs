namespace WebApiAuthentication.DTOs;

public class RefreshDto
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}
