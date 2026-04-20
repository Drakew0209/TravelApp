namespace TravelApp.Services.Abstractions;

public interface IPoiQrCodeService
{
    string BuildPoiShareLink(int poiId, string? languageCode = null);

    byte[] GeneratePoiQrCodePng(int poiId, string? languageCode = null);

    byte[] GeneratePoiQrCodePng(string content);
}
