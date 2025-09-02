using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Npgsql;
using NpgsqlTypes;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class BattleManagementService(
    ISqlQueryRepository _sqlQueryRepository,
    IMapper mapper,
    IHttpContextAccessor httpContextAccessor,
    IGenericRepository<BattleList> battleListRepository
) : IBattleManagementService
{
    public int UserId => httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

    #region Battle List Data 

    public async Task<List<BattleManagementData>> GetBattleList()
    {
        string query = string.Format(
            SqlConstants.GET_BATTLE_LIST_TEMPLATE,
            SqlConstants.GET_BATTLE_LIST_FUNCTION
        );

        var parameters = new NpgsqlParameter[]
        {
            new("p_permanent_type", NpgsqlDbType.Integer) { Value = (int)BattleType.Permanent },
            new("p_time_limited_type", NpgsqlDbType.Integer) { Value = (int)BattleType.TimeLimited },
            new("p_active_battle_status", NpgsqlDbType.Integer) { Value = (int)BattleCreationStatus.Active },
            new("p_completed_battle_status", NpgsqlDbType.Integer) { Value = (int)BattleCreationStatus.Completed },
            new("p_running_status", NpgsqlDbType.Integer) { Value = (int)Infrastructure.Enums.BattleStatus.Running }
        };

        // Execute query
        List<BattleManagementData> battleList =
            await _sqlQueryRepository.SqlQueryListAsync<BattleManagementData>(query, parameters);

        return mapper.Map<List<BattleManagementData>>(battleList);
    }
    #endregion
    #region Create/Update Battle
    public async Task<CreateUpdateResponseDto> CreateUpdateBattle(SaveBattleRequestDTO battleCreateUpdateRequestDto)
    {
        if (battleCreateUpdateRequestDto == null)
            throw new AppException(Constants.INVALID_DATA_MESSAGE);

        string query = string.Format(
            SqlConstants.CREATE_UPDATE_BATTLE_QUERY_TEMPLATE,
            SqlConstants.CREATE_UPDATE_BATTLE_FUNCTION
        );

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var parameters = new NpgsqlParameter[]
        {
        new("p_battle_id", NpgsqlDbType.Integer) { Value = battleCreateUpdateRequestDto.Id ?? (object)DBNull.Value },
        new("p_name", NpgsqlDbType.Text) { Value = battleCreateUpdateRequestDto.Name },
        new("p_description", NpgsqlDbType.Text) { Value = battleCreateUpdateRequestDto.Description },
        new("p_difficulty_level_id", NpgsqlDbType.Integer) { Value = battleCreateUpdateRequestDto.DifficultyLevelId },
        new("p_category_id", NpgsqlDbType.Integer) { Value = battleCreateUpdateRequestDto.CategoryId },
        new("p_status", NpgsqlDbType.Integer) { Value = battleCreateUpdateRequestDto.Status },
        new("p_is_time_limited", NpgsqlDbType.Boolean){Value = (battleCreateUpdateRequestDto.BattleType == (int)BattleType.TimeLimited)},
        new("p_start_date", NpgsqlDbType.TimestampTz) { Value = (object?)battleCreateUpdateRequestDto.StartDate ?? DBNull.Value },
        new("p_end_date", NpgsqlDbType.TimestampTz) { Value = (object?)battleCreateUpdateRequestDto.EndDate ?? DBNull.Value },
        new("p_total_time", NpgsqlDbType.Numeric) { Value = battleCreateUpdateRequestDto.TotalTime },
        new("p_total_question", NpgsqlDbType.Integer) { Value = battleCreateUpdateRequestDto.TotalQuestion },
        new("p_total_xp", NpgsqlDbType.Integer) { Value = battleCreateUpdateRequestDto.TotalXp },
        new("p_questions", NpgsqlDbType.Jsonb)
        {
            Value = battleCreateUpdateRequestDto.Questions != null
                ? JsonSerializer.Serialize(battleCreateUpdateRequestDto.Questions, jsonOptions)
                : "[]"
        },
        new("p_question_difficulty", NpgsqlDbType.Jsonb)
        {
            Value = battleCreateUpdateRequestDto.QuestionsDifficulty != null
                ? JsonSerializer.Serialize(battleCreateUpdateRequestDto.QuestionsDifficulty, jsonOptions)
                : "[]"
        },
        new("p_quiz_types", NpgsqlDbType.Integer) { Value = battleCreateUpdateRequestDto.QuizTypes },
        new("p_created_by", NpgsqlDbType.Integer) { Value = UserId }
        };

        CreateUpdateResponseDto response = await _sqlQueryRepository.SqlQuerySingleAsync<CreateUpdateResponseDto>(query, parameters);

        if (response == null)
            throw new AppException(Constants.CREATE_OR_UPDATE_BATTLE_FAILED, 500);

        if (!response.Success)
            throw new AppException(response.Message, 400);

        return response;
    }
    #endregion

    #region Get Battle By Id
    public async Task<BattleResponseDto> GetBattleById(int battleId)
    {
        if (!await battleListRepository.Exists(b => b.Id == battleId && !b.IsDeleted))
            throw new AppException(string.Format(Constants.BATTLE_NOT_FOUND, battleId));

        string query = string.Format(
            SqlConstants.GET_BATTLE_DATA_BY_ID_QUERY_TEMPLATE,
            SqlConstants.GET_BATTLE_DATA_BY_ID_FUNCTION
        );

        var parameters = new NpgsqlParameter[]
        {
            new("p_battle_id", NpgsqlDbType.Integer) { Value = battleId }
        };

        return mapper.Map<BattleResponseDto>(await _sqlQueryRepository.SqlQuerySingleAsync<BattleResponseDto>(query, parameters));
    }
    #endregion

    #region Delete Battle
    public async Task<string> DeleteBattle(int battleId)
    {
        var battle = await battleListRepository.GetAsync(b => b.Id == battleId && !b.IsDeleted)
            ?? throw new AppException(string.Format(Constants.BATTLE_NOT_FOUND, battleId));

        // Soft delete battle
        battle.IsDeleted = true;
        battle.ModifiedBy = UserId;
        battle.ModifiedDate = DateTime.UtcNow;

        await battleListRepository.UpdateAsync(battle);

        return Constants.DELETE_SUCCESS;
    }
    #endregion

}