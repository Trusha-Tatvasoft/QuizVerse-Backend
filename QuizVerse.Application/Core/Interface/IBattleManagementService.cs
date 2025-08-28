using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IBattleManagementService
{
    Task<List<BattleManagementData>> GetBattleList();

    Task<CreateUpdateResponseDto> CreateUpdateBattle(SaveBattleRequestDTO battleCreateUpdateRequestDto);

    Task<BattleResponseDto> GetBattleById(int battleId);

    Task<string> DeleteBattle(int battleId);
}
