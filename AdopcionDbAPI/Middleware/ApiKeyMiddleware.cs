namespace AdopcionDbAPI.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
        {
            if (
                context.Request.Method == "OPTIONS" ||
                context.Request.Path.StartsWithSegments("/swagger") ||
                context.Request.Path.StartsWithSegments("/api/Views/available-pets") ||
                (
                    context.Request.Path.StartsWithSegments("/api/PetImages") &&
                    context.Request.Method == "GET"
                )
            )
            {
                await _next(context);
                return;
            }

            var extractedApiKey = context.Request.Headers
                .FirstOrDefault(h =>
                    h.Key.Equals("X-API-KEY", StringComparison.OrdinalIgnoreCase) ||
                    h.Key.Equals("X-Api-Key", StringComparison.OrdinalIgnoreCase))
                .Value
                .ToString();

            if (string.IsNullOrWhiteSpace(extractedApiKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "API Key requerida" });
                return;
            }

            var apiKey = configuration["ApiSettings:ApiKey"];

            if (
                string.IsNullOrWhiteSpace(apiKey) ||
                extractedApiKey.Trim() != apiKey.Trim()
            )
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "API Key inválida" });
                return;
            }

            await _next(context);
        }
    }
}