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
    public async Task<MemoryStream> UserExportData(PageListRequest query)
    {
        IQueryable<User> userQuery = GetUserData(query);
        List<UserExportDto> users = await userQuery.ProjectTo<UserExportDto>(mapper.ConfigurationProvider).ToListAsync();
        if (users.Count == 0) throw new AppException(Constants.USER_DATA_NULL);

        XLWorkbook workbook = new(); IXLWorksheet ws = workbook.Worksheets.Add("Users");
        string logoPath = "wwwroot/images/logo.png";
        if (File.Exists(logoPath)) ws.AddPicture(logoPath).MoveTo(ws.Cell("C2")).WithSize(300, 70);

        (string, string)[] meta = {
            ("Search Text:", string.IsNullOrWhiteSpace(query.SearchTerm) ? "-" : query.SearchTerm),
            ("Total Records:", users.Count.ToString()),
            ("Filter:", $"Role: {(query.Filters?.Role?.ToString() ?? "All")}, Status: {(query.Filters?.Status?.ToString() ?? "All")}")
        };

        int labelCol = 1, valueCol = 2;
        for (int i = 0; i < meta.Length; i++)
        {
            string label = meta[i].Item1; string value = meta[i].Item2;
            ws.Cell(7, labelCol).Value = label; ApplyLabelStyle(ws.Cell(7, labelCol));
            if (i == 2)
            {
                IXLRange merged = ws.Range(7, valueCol, 7, valueCol + 1);
                merged.Merge(); merged.Value = value; ApplyValueStyle(merged.FirstCell());
                labelCol += 4; valueCol += 4;
            }
            else
            {
                ws.Cell(7, valueCol).Value = value; ApplyValueStyle(ws.Cell(7, valueCol));
                labelCol += 3; valueCol += 3;
            }
        }

        string[] headers = { "No.", "Full Name", "Email", "Username", "Total Quiz", "Role", "Status", "Join Date", "Last Active" };
        for (int col = 1; col <= headers.Length; col++) ApplyTableHeaderStyle(ws.Cell(9, col).SetValue(headers[col - 1]));

        int row = 10;
        foreach (KeyValuePair<UserExportDto, int> entry in users.Select((x, i) => new KeyValuePair<UserExportDto, int>(x, i + 1)))
        {
            UserExportDto u = entry.Key;
            int i = entry.Value;

            for (int col = 1; col <= 9; col++)
                ws.Cell(row, col).Style.Fill.BackgroundColor = row % 2 == 0 ? XLColor.FromHtml("#FAF6FE") : XLColor.FromHtml("#BDD2FF");

            ApplyDataCell(ws.Cell(row, 1), i);
            ApplyDataCell(ws.Cell(row, 2), u.FullName ?? "-");
            ApplyDataCell(ws.Cell(row, 3), u.Email ?? "-");
            ApplyDataCell(ws.Cell(row, 4), u.UserName ?? "-");
            ApplyDataCell(ws.Cell(row, 5), u.TotalQuizAttemptedCount);
            ApplyDataCell(ws.Cell(row, 6), u.RoleName ?? "-");
            ApplyDataCell(ws.Cell(row, 7), u.StatusName ?? "-");
            ApplyDataCell(ws.Cell(row, 8), u.JoinDate.ToString("dd-MM-yyyy"));
            ApplyDataCell(ws.Cell(row, 9), u.LastActive?.ToString("dd-MM-yyyy") ?? "-");
            row++;
        }

        MemoryStream stream = new();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private void ApplyLabelStyle(IXLCell cell)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563eb");
        cell.Style.Font.FontColor = XLColor.White;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }

    private void ApplyValueStyle(IXLCell cell)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }

    private void ApplyTableHeaderStyle(IXLCell cell)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563eb");
        cell.Style.Font.FontColor = XLColor.White;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    private void ApplyDataCell(IXLCell cell, object value)
    {
        cell.Value = value?.ToString() ?? "-";
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }
    #endregion


}
