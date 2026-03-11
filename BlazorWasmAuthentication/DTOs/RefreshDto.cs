using System.ComponentModel.DataAnnotations;

namespace BlazorWasmAuthentication.DTOs;

public record RefreshDto(
    [Required] string AccessToken,
    [Required] string RefreshToken);

