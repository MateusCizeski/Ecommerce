using System.Diagnostics;

namespace Application.Common.Behaviors;

public class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;
    private static readonly string RequestName = typeof(TRequest).Name;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var timer = Stopwatch.StartNew();

        try
        {
            return await next();
        }
        finally
        {
            timer.Stop();
            var elapsedMilliseconds = timer.ElapsedMilliseconds;

            if (elapsedMilliseconds > SlowRequestThresholdMs)
            {
                logger.LogWarning(
                    "Alerta de Performance: A requisição {RequestName} demorou {ElapsedMs}ms. Detalhes: {@Request}",
                    RequestName, elapsedMilliseconds, request);
            }
        }
    }
}