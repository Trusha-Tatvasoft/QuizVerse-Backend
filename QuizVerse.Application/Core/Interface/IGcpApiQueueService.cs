using QuizVerse.Infrastructure.DTOs.RequestDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IGcpApiQueueService
{
    Task EnqueueAsync(GcpApiReportDataDto data);
    Task StartProcessingAsync(CancellationToken cancellationToken);
}
