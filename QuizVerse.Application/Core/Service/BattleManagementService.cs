using AutoMapper;
using Npgsql;
using NpgsqlTypes;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class BattleManagementService(ISqlQueryRepository _sqlQueryRepository, IMapper mapper) : IBattleManagementService
{
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
            new("p_running_status", NpgsqlDbType.Integer) { Value = (int)BattleStatus.Running }
        };

        // Execute query
        List<BattleManagementData> battleList =
            await _sqlQueryRepository.SqlQueryListAsync<BattleManagementData>(query, parameters);

        return mapper.Map<List<BattleManagementData>>(battleList);
    }
    #endregion

}
