using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

namespace QuizVerse.Application.Core.Service;

public class UserService(IGenericRepository<User> userRepository, ICommonService commonService, IEmailService emailService, IMapper mapper, IHttpContextAccessor httpContextAccessor) : IUserService
{
    public int? UserId => httpContextAccessor.HttpContext?.User?.GetUserId();

    #region User Queries
    private IQueryable<User> GetUserData(PageListRequest query)
    {
        IQueryable<User> userQuery = userRepository.GetQueryableInclude(u => u.Role).Where(u => !u.IsDeleted);

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
        if (dto.Id.HasValue && dto.Id.Value > 0)
        {
            // UPDATE
            var user = await userRepository.GetAsync(u => u.Id == dto.Id && !u.IsDeleted)
                ?? throw new AppException(string.Format(Constants.USER_NOT_FOUND, dto.Id));

            if (await userRepository.Exists(u => u.Email == dto.Email && u.Id != dto.Id))
                throw new AppException(Constants.DUPLICATE_EMAIL);

            if (await userRepository.Exists(u => u.UserName == dto.UserName && u.Id != dto.Id))
                throw new AppException(Constants.DUPLICATE_USERNAME);

            mapper.Map(dto, user);
            user.ModifiedBy = UserId;
            user.ModifiedDate = DateTime.UtcNow;

            await userRepository.UpdateAsync(user);
            return (true, Constants.UPDATE_SUCCESS);
        }
        else
        {
            // CREATE
            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new AppException(Constants.PASSWORD_REQUIRED_FOR_NEW_USER);

            if (await userRepository.Exists(u => u.Email == dto.Email))
                throw new AppException(Constants.DUPLICATE_EMAIL);

            if (await userRepository.Exists(u => u.UserName == dto.UserName))
                throw new AppException(Constants.DUPLICATE_USERNAME);

            var user = mapper.Map<User>(dto);
            user.Password = commonService.Hash(dto.Password);
            user.RoleId = (int)UserRoles.Player;
            user.Status = (int)UserStatus.Active;
            user.CreatedDate = DateTime.UtcNow;
            user.CreatedBy = UserId;
            user.FirstTimeLogin = true;

            await userRepository.AddAsync(user);

            string emailStatus = await SendMailToNewUser(user.Email, dto.Password);

            return (true, string.Format(Constants.CREATE_SUCCESS + ". " + emailStatus));
        }
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
                break;

            default:
                throw new AppException(Constants.INVALID_DATA_MESSAGE);
        }

        await userRepository.UpdateAsync(user);

        return resultMessage;
    }
    #endregion

    #region Send mail
    private async Task<string> SendMailToNewUser(string email, string plainPassword)
    {
        string? templatePath = Constants.NEW_USER_TEMPLATE_PATH;
        if (string.IsNullOrWhiteSpace(templatePath))
            throw new AppException(Constants.EMAIL_PATH_NOT_CONFIGURED);

        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), templatePath);
        string emailBody = await File.ReadAllTextAsync(fullPath);

        emailBody = emailBody.Replace("{username}", email);
        emailBody = emailBody.Replace("{password}", plainPassword);

        bool isEmailSent = await emailService.SendEmailAsync(new EmailRequestDto
        {
            To = email,
            Subject = Constants.QUIZVERSE_DEFAULT_QUOTE,
            Body = emailBody
        });

        if (isEmailSent)
            return string.Format(Constants.EMAIL_SENT_SUCCESS, email);
        else
            return Constants.EMAIL_NOT_SENT;

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
