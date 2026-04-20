using System.ComponentModel.DataAnnotations;

namespace TravelApp.Application.Dtos.Auth;

public sealed record RegisterRequestDto(
    [param: Required, EmailAddress, StringLength(255)] string Email,
    [param: Required, MinLength(8), StringLength(128)] string Password,
    [param: Required, MinLength(3), StringLength(200)] string FullName);
