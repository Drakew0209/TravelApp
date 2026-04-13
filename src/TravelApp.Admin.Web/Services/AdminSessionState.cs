namespace TravelApp.Admin.Web.Services;

public sealed class AdminSessionState
{
    public DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;
}
