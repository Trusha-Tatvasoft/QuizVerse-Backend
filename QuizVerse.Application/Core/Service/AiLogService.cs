using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class AiLogService(IGenericRepository<AiProcessLog> _aiProcessLogRepository) : IAiLogService
{
    public AiProcessLog StartApiCall(AiApiCallStartDetail aiApiCallStartDetail)
    {
        AiProcessLog aiProcessLog = new AiProcessLog
        {
            ModelName = aiApiCallStartDetail.ModelName,
            StartTime = DateTime.UtcNow,
            Purpose = aiApiCallStartDetail.Purpose,
            IsSuccess = false
        };

        if (!string.IsNullOrEmpty(aiApiCallStartDetail.ExtraInfo))
        {
            aiProcessLog.ExtraInfo = aiApiCallStartDetail.ExtraInfo;
        }

        return aiProcessLog;
    }

    public async Task EndApiCall(AiProcessLog aiProcessLog, bool isSuccess)
    {
        aiProcessLog.EndTime = DateTime.UtcNow;
        aiProcessLog.IsSuccess = isSuccess;

        await _aiProcessLogRepository.AddAsync(aiProcessLog);
    }
}
