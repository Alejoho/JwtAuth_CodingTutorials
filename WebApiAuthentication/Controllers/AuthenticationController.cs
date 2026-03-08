using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApiAuthentication.Authentication;
using WebApiAuthentication.DTOs;

namespace WebApiAuthentication.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthenticationController(
    UserManager<LibraryUser> userManager,
    IConfiguration _config,
    ILogger<AuthenticationController> logger) : ControllerBase
{
    private readonly UserManager<LibraryUser> _userManager = userManager;
    private readonly IConfiguration _config = _config;
    private readonly ILogger<AuthenticationController> _logger = logger;

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegistrationDto dto)
    {
        _logger.LogInformation("Register called");

        var existingUser = await _userManager.FindByNameAsync(dto.Username);

        if (existingUser != null)
        {
            return Conflict("User already exists.");
        }

        var newUser = new LibraryUser
        {
            Reviews = [],
            UserName = dto.Username,
            Email = dto.Email,
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        var result = await _userManager.CreateAsync(newUser, dto.Password);

        if (result.Succeeded == false)
        {
            _logger.LogInformation("Register succeeded");

            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Failed to create the new user: {string.Join(" ", result.Errors.Select(e => e.Description))}");
        }

        return Ok("User successfully created");
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto dto)
    {
        _logger.LogInformation("Login called");

        var user = await _userManager.FindByNameAsync(dto.Username);

        if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
        {
            return Unauthorized();
        }

        var token = GenerateJwt(dto.Username);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = GenerateRefreshToken();

        await SetRefreshTokenToUser(user, refreshToken);

        _logger.LogInformation("Login succeeded");

        return Ok(new LoginResponseDto
        {
            JwtToken = jwt,
            Expiration = token.ValidTo,
            RefreshToken = refreshToken
        });
    }
    private JwtSecurityToken GenerateJwt(string username)
    {
        List<Claim> authClaims = [
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())];

        var audiences = _config.GetSection("Jwt:ValidAudiences").Get<List<string>>()
            ?? throw new InvalidOperationException("Audiences not configured");

        foreach (var audience in audiences)
        {
            authClaims.Add(new Claim(JwtRegisteredClaimNames.Aud, audience));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Secret not configured")));

        int expLapse = Convert.ToInt32(_config["Jwt:SecondsToExpire"]
                ?? throw new InvalidOperationException("Expiration lapse for jwt not configured"));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:ValidIssuer"],
            expires: DateTime.UtcNow.AddSeconds(expLapse),
            claims: authClaims,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return token;
    }

        _logger.LogInformation("Login succeeded");

        return Ok(new LoginResponseDto
        {
            JwtToken = jwt,
            Expiration = token.ValidTo
        });
    }
}
