using System.Diagnostics;

namespace Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private static readonly string RequestName = typeof(TRequest).Name;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        logger.LogInformation("Iniciando requisição: {RequestName}", RequestName);

        var timer = Stopwatch.StartNew();

        try
        {
            var response = await next();

            timer.Stop();
            logger.LogInformation("Requisição {RequestName} finalizada com sucesso em {ElapsedMs}ms",
                RequestName, timer.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            timer.Stop();
            logger.LogError(ex, "Falha na requisição {RequestName} após {ElapsedMs}ms",
                RequestName, timer.ElapsedMilliseconds);
            throw;
        }
    }
}