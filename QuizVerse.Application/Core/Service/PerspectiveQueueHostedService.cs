using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuizVerse.Application.Core.Interface;

namespace QuizVerse.Application.Core.Service;
public class PerspectiveQueueHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PerspectiveQueueHostedService> _logger;
    private readonly TimeSpan _startupDelay;

    public PerspectiveQueueHostedService(
        IServiceProvider serviceProvider,
        ILogger<PerspectiveQueueHostedService> logger,
        TimeSpan? startupDelay = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _startupDelay = startupDelay ?? TimeSpan.FromSeconds(5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Perspective Queue Hosted Service is starting");

        await Task.Delay(_startupDelay, stoppingToken);

        using var scope = _serviceProvider.CreateScope();
        var queueManager = scope.ServiceProvider.GetRequiredService<IGcpApiQueueService>();

        try
        {
            await queueManager.StartProcessingAsync(stoppingToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "Perspective Queue Hosted Service is stopping gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Perspective Queue Hosted Service");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Perspective Queue Hosted Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
