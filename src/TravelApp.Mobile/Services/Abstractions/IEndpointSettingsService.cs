namespace TravelApp.Services.Abstractions;

public interface IEndpointSettingsService
{
    string ApiBaseUrl { get; }
    string PublicWebBaseUrl { get; }

    event EventHandler? SettingsChanged;

    void Update(string apiBaseUrl, string publicWebBaseUrl);
    void ResetToDefaults();
}
