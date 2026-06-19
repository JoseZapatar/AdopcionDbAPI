namespace AdopcionDbAPI.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string HeaderName = "X-API-KEY";


    public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IConfiguration configuration)
        {
            if (!context.Request.Headers.TryGetValue(
                HeaderName,
                out var apiKeyHeader))
            {
                context.Response.StatusCode = 401;

                await context.Response.WriteAsJsonAsync(
                    new
                    {
                        message = "API Key requerida"
                    });

                return;
            }

            var apiKey =
                configuration["ApiSettings:ApiKey"];

            if (apiKey != apiKeyHeader)
            {
                context.Response.StatusCode = 403;

                await context.Response.WriteAsJsonAsync(
                    new
                    {
                        message = "API Key inválida"
                    });

                return;
            }

            await _next(context);
        }
    }


}
