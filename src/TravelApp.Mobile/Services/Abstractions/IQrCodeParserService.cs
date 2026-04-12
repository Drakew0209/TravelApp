namespace TravelApp.Services.Abstractions;

public interface IQrCodeParserService
{
    int? TryParsePoiId(string? qrContent);
}
