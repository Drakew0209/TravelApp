using System.Net.Http.Json;
using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Api;

public class ProfileApiClient : ApiClientBase, IProfileApiClient
{
    public ProfileApiClient(IHttpClientFactory httpClientFactory, ApiClientOptions options, ITokenStore tokenStore)
        : base(httpClientFactory, options, tokenStore)
    {
    }

    public async Task<ProfileDto?> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, "api/auth/profile"), authorized: true, cancellationToken);
        return await ReadAsAsync<ProfileDto>(response, cancellationToken);
    }

    public async Task<bool> UpdateMyProfileAsync(UpdateProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Put, "api/auth/profile")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        }, authorized: true, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
