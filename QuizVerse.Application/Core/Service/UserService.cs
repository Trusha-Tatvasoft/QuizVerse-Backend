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

        List<UserExportDto> users = await userQuery
                                        .ProjectTo<UserExportDto>(mapper.ConfigurationProvider)
                                        .ToListAsync();

        if (users.Count == 0)
        {
            throw new AppException(Constants.USER_DATA_NULL);
        }

        XLWorkbook workbook = new();
        IXLWorksheet worksheet = workbook.Worksheets.Add("Users");

        // === Logo Insert ===

        string logoPath = "wwwroot/images/logo.png";
        if (System.IO.File.Exists(logoPath))
        {
            IXLCell startCell = worksheet.Cell("G2");
            worksheet.AddPicture(logoPath)
                     .MoveTo(startCell)
                     .WithSize(320, 70);
        }

        // === Header Info ===
        worksheet.Range("A7:B8").Merge().Value = "Search Text:";
        worksheet.Range("C7:E8").Merge().Value = string.IsNullOrWhiteSpace(query.SearchTerm) ? "-" : query.SearchTerm;

        worksheet.Range("G7:H8").Merge().Value = "Total Records:";
        worksheet.Range("I7:K8").Merge().Value = users.Count;

        worksheet.Range("M7:N8").Merge().Value = "Filter:";
        string filterText = $"Role: {query.Filters?.Role?.ToString() ?? "All"}, Status: {query.Filters?.Status?.ToString() ?? "All"}";
        worksheet.Range("O7:Q8").Merge().Value = filterText;

        // === Header Styling ===
        IXLRange[] labels = {
            worksheet.Range("A7:B8"),
            worksheet.Range("G7:H8"),
            worksheet.Range("M7:N8")
        };

        IXLRange[] values = {
            worksheet.Range("C7:E8"),
            worksheet.Range("I7:K8"),
            worksheet.Range("O7:Q8")
        };

        foreach (IXLRange label in labels)
        {
            label.Style.Font.Bold = true;
            label.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563eb");
            label.Style.Font.FontColor = XLColor.White;
            label.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            label.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            label.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        foreach (IXLRange value in values)
        {
            value.Style.Font.Bold = true;
            value.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            value.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            value.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // === Table Header ===
        int headerRow = 10;
        worksheet.Cell(headerRow, 1).Value = "No.";
        worksheet.Range(headerRow, 2, headerRow, 3).Merge().Value = "Full Name";
        worksheet.Range(headerRow, 4, headerRow, 6).Merge().Value = "Email";
        worksheet.Range(headerRow, 7, headerRow, 8).Merge().Value = "Username";
        worksheet.Range(headerRow, 9, headerRow, 10).Merge().Value = "Total Quiz";
        worksheet.Range(headerRow, 11, headerRow, 12).Merge().Value = "Role";
        worksheet.Cell(headerRow, 13).Value = "Status";
        worksheet.Range(headerRow, 14, headerRow, 15).Merge().Value = "Join Date";
        worksheet.Range(headerRow, 16, headerRow, 17).Merge().Value = "Last Active";

        IXLRange tableHeader = worksheet.Range(headerRow, 1, headerRow, 17);
        tableHeader.Style.Font.Bold = true;
        tableHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563eb");
        tableHeader.Style.Font.FontColor = XLColor.White;
        tableHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        tableHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        tableHeader.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableHeader.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        // === Insert Rows ===
        int row = headerRow + 1;
        for (int i = 0; i < users.Count; i++)
        {
            UserExportDto u = users[i];

            worksheet.Cell(row, 1).Value = i + 1;
            worksheet.Range(row, 2, row, 3).Merge().Value = u.FullName;
            worksheet.Range(row, 4, row, 6).Merge().Value = u.Email;
            worksheet.Range(row, 7, row, 8).Merge().Value = u.UserName;
            worksheet.Range(row, 9, row, 10).Merge().Value = u.TotalQuizAttemptedCount;
            worksheet.Range(row, 11, row, 12).Merge().Value = u.RoleName;
            worksheet.Cell(row, 13).Value = u.StatusName;
            worksheet.Range(row, 14, row, 15).Merge().Value = u.JoinDate.ToString("dd-MM-yyyy");
            worksheet.Range(row, 16, row, 17).Merge().Value = u.LastActive.HasValue
                ? u.LastActive.Value.ToString("dd-MM-yyyy")
                : "-";

            IXLRange dataRow = worksheet.Range(row, 1, row, 17);
            dataRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            dataRow.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dataRow.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRow.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            row++;
        }

        // === Return MemoryStream ===
        MemoryStream memoryStream = new();
        workbook.SaveAs(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }
    #endregion
}
