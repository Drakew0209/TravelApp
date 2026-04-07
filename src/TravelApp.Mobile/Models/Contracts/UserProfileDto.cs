namespace TravelApp.Models.Contracts;

public record UserProfileDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName = "");
