using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TravelApp.Public.Web.Services;

namespace TravelApp.Public.Web.Pages.Media;

public sealed class AudioModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<TravelAppApiOptions> _options;

    public AudioModel(IHttpClientFactory httpClientFactory, IOptions<TravelAppApiOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public async Task<IActionResult> OnGetAsync(string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url) || !TryNormalizeSourceUrl(url, out var sourceUrl))
        {
            return BadRequest();
        }

        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(sourceUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            return StatusCode((int)response.StatusCode);
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return new AudioProxyResult(stream, response.Content.Headers.ContentType?.MediaType ?? "audio/mpeg", response);
    }

    private bool TryNormalizeSourceUrl(string url, out Uri sourceUrl)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
        {
            var allowedBase = new Uri(_options.Value.BaseUrl, UriKind.Absolute);
            if (string.Equals(absolute.Host, allowedBase.Host, StringComparison.OrdinalIgnoreCase)
                || string.Equals(absolute.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                sourceUrl = absolute;
                return true;
            }
        }
        else if (Uri.TryCreate(new Uri(_options.Value.BaseUrl, UriKind.Absolute), url, out var relative))
        {
            sourceUrl = relative;
            return true;
        }

        sourceUrl = default!;
        return false;
    }

    private sealed class AudioProxyResult : IActionResult
    {
        private readonly Stream _stream;
        private readonly string _contentType;
        private readonly HttpResponseMessage _response;

        public AudioProxyResult(Stream stream, string contentType, HttpResponseMessage response)
        {
            _stream = stream;
            _contentType = contentType;
            _response = response;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.ContentType = _contentType;

            if (_response.Content.Headers.ContentLength.HasValue)
            {
                context.HttpContext.Response.ContentLength = _response.Content.Headers.ContentLength.Value;
            }

            using (_response)
            await using (_stream)
            {
                await _stream.CopyToAsync(context.HttpContext.Response.Body, context.HttpContext.RequestAborted);
            }
        }
    }
}
