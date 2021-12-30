using Microsoft.Extensions.Primitives;
using System.Linq;
using System.Diagnostics;

namespace Bios
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class APIKeyCheckMiddleware
    {
        private readonly RequestDelegate _next;
        private const string COMPANY_HEADER = "company";
        private const string CHANNEL_HEADER = "channel";
        private const string KEY_HEADER = "key";
        private const string API_KEY_HEADER = "api-key";

        public APIKeyCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(COMPANY_HEADER, out var extractedCompany))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync($"Api Key was not found in request. Please pass key in {COMPANY_HEADER} header.");
                return;
            }

            if (!context.Request.Headers.TryGetValue(CHANNEL_HEADER, out var extractedChannel))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync($"Api Key was not found in request. Please pass key in {CHANNEL_HEADER} header.");
                return;
            }

            if (!context.Request.Headers.TryGetValue(KEY_HEADER, out var extractedKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync($"Api Key was not found in request. Please pass key in {KEY_HEADER} header.");
                return;
            }


           var appSettings = context.RequestServices.GetRequiredService<IConfiguration>();
           var validApiKey = appSettings.GetSection(API_KEY_HEADER).Get<ApiKey[]>();

            Debug.WriteLine(validApiKey.Length);
            Debug.WriteLine(validApiKey[0].key);
            if (!Array.Exists<ApiKey>(validApiKey, apiKey => apiKey.company == extractedCompany && apiKey.channel == extractedChannel && apiKey.key == extractedKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid api key.");
                return;
            }

            await _next(context);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class APIKeyCheckMiddlewareExtensions
    {
        public static IApplicationBuilder UseAPIKeyCheckMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<APIKeyCheckMiddleware>();
        }
    }
}
