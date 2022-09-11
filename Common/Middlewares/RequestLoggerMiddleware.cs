using System.Net.Mime;
using System.Text;
using MiniApp.Core.Repositories;
using MiniApp.Dtos.RequestLog;

namespace MiniApp.Common.Middlewares;

public class RequestLoggerMiddleware
{
    private readonly RequestDelegate _next;

    private readonly ILogger<RequestLoggerMiddleware> _logger;

    private readonly IRequestLogRepository _requestLogRepository;

    public RequestLoggerMiddleware(RequestDelegate next, ILogger<RequestLoggerMiddleware> logger, IRequestLogRepository requestLogRepository)
    {
        _next = next;
        _logger = logger;
        _requestLogRepository = requestLogRepository;
    }
    
    public async Task InvokeAsync(HttpContext httpContext)
    {
        var originalBody = httpContext.Response.Body;

        httpContext.Request.EnableBuffering();

        using var requestReader = new StreamReader(httpContext.Request.Body, Encoding.UTF8);
        
        var requestBody = await requestReader.ReadToEndAsync();
        
        httpContext.Request.Body.Position = 0;
        
        try
        {
            using var memStream = new MemoryStream();
            httpContext.Response.Body = memStream;

            await _next(httpContext);

            memStream.Position = 0;
            
            var responseBody = await new StreamReader(memStream).ReadToEndAsync();

            memStream.Position = 0;
            
            await memStream.CopyToAsync(originalBody);
            
            await LogRequestData(httpContext, requestBody, responseBody);
            
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(httpContext, requestBody, exception);
        }
        finally
        {
            httpContext.Response.Body = originalBody;
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext httpContext, string requestBody, Exception exception)
    {
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;
        httpContext.Response.StatusCode = 500;

        _logger.LogError(exception, "{Message}", exception.Message);
        
        await LogRequestData(httpContext, requestBody, null, exception.Message);
    }

    private async Task LogRequestData(HttpContext httpContext, string requestBody, string? responseBody, string? exceptionMessage = null)
    {
        var createRequestLogDto = new CreateRequestLogDto
        {
            QueryString = httpContext.Request.QueryString.ToString(),
            Body = requestBody,
            Path = httpContext.Request.Path,
            Method = httpContext.Request.Method,
            Response = responseBody,
            Exception = exceptionMessage
        };

        await _requestLogRepository.LogAsync(createRequestLogDto);
    }
}