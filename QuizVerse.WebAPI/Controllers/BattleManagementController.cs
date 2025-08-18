using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BattleManagementController(IBattleManagementService battleManagementService) : ControllerBase
{
    #region Battle List Data 
    [HttpPost("get-battle-list")]
    public async Task<IActionResult> GetBattleList()
    {
        return Ok(new ApiResponse<List<BattleManagementData>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await battleManagementService.GetBattleList()
        });
    }
    #endregion
}
