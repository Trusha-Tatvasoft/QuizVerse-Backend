using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IBattleManagementService
{
    Task<BattleManagementDataResponseDto> GetBattleList(int battachNumber);

    Task<CreateUpdateResponseDto> CreateUpdateBattle(SaveBattleRequestDTO battleCreateUpdateRequestDto);

    Task<BattleResponseDto> GetBattleById(int battleId);

    Task<string> DeleteBattle(int battleId);
}
