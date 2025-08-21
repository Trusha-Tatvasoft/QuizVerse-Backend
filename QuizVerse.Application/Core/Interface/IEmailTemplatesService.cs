using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IEmailTemplatesService
{
    PageListResponse<EmailTemplatesResponseDto> GetAllEmailTemplates(PageListRequest pageListRequest);
}