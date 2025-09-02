using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IPlatformConfigurationService
{
    Task<PlateformConfigurationResponseDTO> GetPlatformConfigurations();
    Task<string> UpdatePlatformConfigurations(PlatformConfigurationRequestDTO platformConfigurationRequest);
}
