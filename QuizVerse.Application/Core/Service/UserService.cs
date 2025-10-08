using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using System.Linq.Dynamic.Core;
using ClosedXML.Excel;
using Npgsql;
using NpgsqlTypes;
using Microsoft.Extensions.Configuration;

namespace QuizVerse.Application.Core.Service;

public class UserService(IGenericRepository<User> userRepository, ICommonService commonService, IMapper mapper, IHttpContextAccessor httpContextAccessor, ISqlQueryRepository sqlQueryRepository, IConfiguration configuration) : IUserService
{
    public int UserId => httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

    #region User Queries
    private IQueryable<User> GetUserData(PageListRequest query)
    {
        IQueryable<User> userQuery = userRepository.GetQueryableInclude(u => u.Role).Where(u => !u.IsDeleted && u.Id != UserId);

        // Search
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            userQuery = userQuery.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                u.UserName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term));
        }

        // Filters
        var filters = query.Filters;
        if (filters != null)
        {
            if (filters.Status.HasValue)
            {
                if (Enum.IsDefined(typeof(UserStatus), filters.Status.Value))
                {
                    userQuery = userQuery.Where(u => u.Status == (int)filters.Status.Value);
                }
                else
                {
                    throw new AppException(Constants.INVALID_STATUS_MESSAGE);
                }
            }

            if (filters.Role.HasValue)
            {
                if (Enum.IsDefined(typeof(UserRoles), filters.Role.Value))
                {
                    userQuery = userQuery.Where(u => u.RoleId == (int)filters.Role.Value);
                }
                else
                {
                    throw new AppException(Constants.INVALID_ROLE_MESSAGE);
                }
            }
        }

        // Sorting
        if (!string.IsNullOrEmpty(query.SortColumn))
        {
            if (query.SortColumn == "quizattempt")
                query.SortColumn = "QuizAttempteds.Count()";
            userQuery = userQuery.OrderBy($"{query.SortColumn} {(query.SortDescending ? "desc" : "asc")}");
        }
        else
        {
            userQuery = userQuery.OrderBy("Id asc");
        }
        return userQuery;
    }
    #endregion

    #region GetAllUsers
    public async Task<PageListResponse<UserDto>> GetUsersByPagination(PageListRequest pageListRequest)
    {
        IQueryable<User>? userQuery = GetUserData(pageListRequest);
        return await userRepository.PaginatedList<UserDto>(userQuery, pageListRequest, q => q.ProjectTo<UserDto>(mapper.ConfigurationProvider));
    }
    #endregion

    #region GetUserById
    public async Task<UserDto> GetUserById(int id)
    {
        var user = await userRepository.GetAsync(u => u.Id == id && !u.IsDeleted)
            ?? throw new AppException(string.Format(Constants.USER_NOT_FOUND, id));

        return mapper.Map<UserDto>(user);
    }
    #endregion

    #region Create Or Update

    public async Task<(bool Success, string Message)> CreateOrUpdateUser(UserRequestDto dto)
    {
        string? imagePath = null;
        string? password = null;

        if (dto.ProfilePic != null && dto.ProfilePic.Length > 0)
        {
            imagePath = await commonService.SaveFile(dto.ProfilePic, "users");
        }

        if (dto.Id == null)
        {
            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                throw new AppException(Constants.PASSWORD_REQUIRED_FOR_NEW_USER);
            }

            password = commonService.Hash(dto.Password);
        }

        var query = string.Format(SqlConstants.CREATE_OR_UPDATE_USER_QUERY_TEMPLATE, SqlConstants.CREATE_OR_UPDATE_USER_FUNCTION);

        var parameters = new NpgsqlParameter[]
        {
            new("p_id", NpgsqlDbType.Integer) { Value = (object?)dto.Id ?? DBNull.Value },
            new("p_full_name", NpgsqlDbType.Text) { Value = dto.FullName },
            new("p_email", NpgsqlDbType.Text) { Value = dto.Email },
            new("p_username", NpgsqlDbType.Text) { Value = dto.UserName },
            new("p_password", NpgsqlDbType.Text) { Value = (object?)password ?? DBNull.Value },
            new("p_profile_pic", NpgsqlDbType.Text) { Value = (object?)imagePath ?? DBNull.Value },
            new("p_bio", NpgsqlDbType.Text) { Value = (object?)dto.Bio ?? DBNull.Value },
            new("p_role_id", NpgsqlDbType.Integer) { Value = dto.RoleId },
            new("p_status_active", NpgsqlDbType.Integer) { Value = (int)UserStatus.Active },
            new("p_status_inactive", NpgsqlDbType.Integer) { Value = (int)UserStatus.Inactive },
            new("p_status_suspended", NpgsqlDbType.Integer) { Value = (int)UserStatus.Suspended },
            new("p_modified_by", NpgsqlDbType.Integer) { Value = !dto.IsRegister ? UserId : DBNull.Value },
            new("p_first_time_login", NpgsqlDbType.Boolean) { Value = !dto.IsRegister },
        };


        var result = await sqlQueryRepository.SqlQuerySingleAsync<CreateUpdateResponseDto>(query, parameters);

        if (!result.Success)
            throw new AppException(result.Message);

        if (result.Message != Constants.USER_CREATE_SUCCESS)
            return (true, result.Message);

        var placeholders = dto.IsRegister
            ? new Dictionary<string, string> // Register - welcome email
            {
                { "{{user}}", dto.Email },
                { "{{email}}", dto.Email },
                { "{{registrationDate}}", DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "{{loginUrl}}", configuration["QuizVerse:LoginUrl"] ?? string.Empty },
                { "{{companyName}}", Constants.PLATFORM_NAME },
                { "{{year}}", DateTime.UtcNow.Year.ToString() }
            }
            : new Dictionary<string, string> // Admin-created user
            {
                { "{{user}}", dto.Email },
                { "{{password}}", dto.Password! },
                { "{{loginUrl}}", configuration["QuizVerse:LoginUrl"] ?? string.Empty },
            };

        var emailDto = new TemplatedEmailRequestDto
        {
            ToEmail = dto.Email,
            TemplateType = dto.IsRegister
                ? EmailTemplateType.WelComeEmail
                : EmailTemplateType.NewUser,
            Placeholders = placeholders
        };

        string emailResult = await commonService.SendEmailFromTemplate(emailDto);
        string expectedMessage = string.Format(Constants.EMAIL_SENT_SUCCESS, emailDto.ToEmail);
        bool emailSent = emailResult == expectedMessage;

        return dto.IsRegister ? emailSent
                ? (true, Constants.USER_REGISTERED_AND_EMAIL_SENT)
                : throw new AppException(Constants.USER_REGISTERED_BUT_EMAIL_NOT_SENT)
            : (true, result.Message + " " + emailResult);

    }
    #endregion

    #region Update by Action
    public async Task<string> UpdateUserByAction(UserActionRequest userActionRequest)
    {
        var user = await userRepository.GetAsync(u => u.Id == userActionRequest.Id && !u.IsDeleted)
            ?? throw new AppException(string.Format(Constants.USER_NOT_FOUND, userActionRequest.Id));

        string resultMessage;

        switch (userActionRequest.Action)
        {
            case UserActionType.Delete:
                if (user.IsDeleted)
                    throw new AppException(string.Format(Constants.USER_ALREADY_DELETED, userActionRequest.Id));

                user.IsDeleted = true;
                user.ModifiedBy = UserId;
                user.ModifiedDate = DateTime.UtcNow;
                resultMessage = Constants.DELETE_SUCCESS;
                break;

            case UserActionType.ChangeStatus:
                if (userActionRequest.NewStatus is null)
                    throw new AppException(Constants.STATUS_REQUIRED);

                if (user.Status == (int)userActionRequest.NewStatus)
                    throw new AppException(string.Format(Constants.STATUS_ALREADY_SET, userActionRequest.NewStatus));

                user.Status = (int)userActionRequest.NewStatus;
                user.ModifiedBy = UserId;
                user.ModifiedDate = DateTime.UtcNow;
                resultMessage = string.Format(Constants.USER_STATUS_CHANGED_SUCCESS, user.Id, userActionRequest.NewStatus);

                // send mail to suspended user
                if (userActionRequest.NewStatus == UserStatus.Suspended)
                {
                    var placeholders = new Dictionary<string, string>
                    {
                        { "{{user}}", user.UserName ?? user.Email },
                        { "{{email}}", user.Email }
                    };

                    var emailDto = new TemplatedEmailRequestDto
                    {
                        TemplateType = EmailTemplateType.AccountSuspension,
                        ToEmail = user.Email,
                        Placeholders = placeholders
                    };

                    await commonService.SendEmailFromTemplate(emailDto);
                }
                break;

            default:
                throw new AppException(Constants.INVALID_DATA_MESSAGE);
        }

        await userRepository.UpdateAsync(user);

        return resultMessage;
    }
    #endregion

    #region User export
    public async Task<MemoryStream> UserExportData(PageListRequest pageListRequest)
    {
        List<UserExportDto> tableData = [.. (await GetUserData(pageListRequest)
                                        .ProjectTo<UserExportDto>(mapper.ConfigurationProvider)
                                        .ToListAsync())
                                        .Select((u, i) => { u.No = i + 1; return u; })];
        if (tableData.Count == 0)
            throw new AppException(Constants.USER_DATA_NULL);

        string role = pageListRequest.Filters?.Role?.ToString() ?? "All";
        string status = pageListRequest.Filters?.Status?.ToString() ?? "All";

        Action<IXLWorksheet> worksheetSetup = worksheet =>
        {
            (string LabelCell, string ValueCell, string Label, string Value)[] headerInfo =
            [
                ("A7", "B7", "Search Text:", string.IsNullOrWhiteSpace(pageListRequest.SearchTerm) ? "-" : pageListRequest.SearchTerm),
                ("D7", "E7", "Total Records:", tableData.Count.ToString()),
                ("G7", "H7", "Filter:", $"Role: {role}, Status: {status}")
            ];

            foreach ((string LabelCell, string ValueCell, string Label, string Value) in headerInfo)
            {
                IXLCell labelCell = worksheet.Cell(LabelCell);
                IXLCell valueCell = worksheet.Cell(ValueCell);

                labelCell.Value = Label;
                valueCell.Value = Value;

                labelCell.Style.Font.Bold = true;
                labelCell.Style.Fill.BackgroundColor = XLColor.FromHtml(Constants.LIGHT_BLUE);
                labelCell.Style.Font.FontColor = XLColor.White;
                labelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                labelCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                labelCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                valueCell.Style.Font.Bold = true;
                valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                valueCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                valueCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
        };

        return commonService.ExportToExcel(tableData, "Users", XLTableTheme.TableStyleMedium9, 10, 1, worksheetSetup);
    }
    #endregion


}
