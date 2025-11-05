using QuizVerse.Domain.Entities;

namespace QuizVerse.Application.Core.Interface;

public interface IAiLogService
{
    AiProcessLog StartApiCall(AiApiCallStartDetail aiApiCallStartDetail);
    Task EndApiCall(AiProcessLog aiProcessLog, bool isSuccess);
}
