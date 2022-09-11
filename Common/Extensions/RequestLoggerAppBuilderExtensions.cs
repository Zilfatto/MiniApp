using MiniApp.Common.Middlewares;

namespace MiniApp.Common.Extensions;

public static class RequestLoggerAppBuilderExtensions
{
    public static void UseRequestLogger(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseMiddleware<RequestLoggerMiddleware>();
    }
}