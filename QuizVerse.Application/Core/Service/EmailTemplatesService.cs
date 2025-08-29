using System.Reflection;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using System.Linq.Dynamic.Core;
using AutoMapper;
using QuizVerse.Infrastructure.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.Enums;
using Ganss.Xss;

namespace QuizVerse.Application.Core.Service;

public class EmailTemplatesService(IGenericRepository<EmailTemplete> emailTemplateRepository, IMapper _mapper, IHttpContextAccessor httpContextAccessor) : IEmailTemplatesService
{
    int UserId => httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new AppException(Constants.INVALID_USER_ID_MESSAGE);

    public PageListResponse<EmailTemplatesResponseDto> GetAllEmailTemplates(PageListRequest pageListRequest)
    {
        IQueryable<EmailTemplete> emailTempletes = emailTemplateRepository.GetQueryableInclude().Where(x => x.IsDeleted == false);
        if (!string.IsNullOrEmpty(pageListRequest.SortColumn))
        {
            // Check if property exists
            var propertyInfo = typeof(EmailTemplete).GetProperty(
                pageListRequest.SortColumn,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
            ) ?? throw new AppException(
                    string.Format(Constants.INVALID_COLUMN_NAME, pageListRequest.SortColumn), 400);

            // If the property is boolean, invert the sort direction
            if (propertyInfo.PropertyType == typeof(bool))
            {
                pageListRequest.SortDescending = !pageListRequest.SortDescending;
            }
            emailTempletes = emailTempletes.OrderBy($"{pageListRequest.SortColumn} {(pageListRequest.SortDescending ? "desc" : "asc")}");
        }
        else
        {
            emailTempletes = emailTempletes.OrderBy(q => q.Id);
        }

        List<EmailTemplatesResponseDto> emailTemplatesResponse = _mapper.Map<List<EmailTemplatesResponseDto>>(emailTempletes.ToList());

        return new PageListResponse<EmailTemplatesResponseDto>
        {
            Records = emailTemplatesResponse,
            TotalRecords = emailTemplatesResponse.Count(),
        };
    }

    public async Task<string> AddOrEditEmailTemplate(EmailTemplatesRequestDTO emailTemplatesRequest)
    {
        // HTML template validations
        if (!IsValidHtmlTemplate(emailTemplatesRequest.Body)) throw new AppException(Constants.INVALID_EMAIL_TEMPLATE, 400);
        string? missingPlaceHolder = GetMissingPlaceholder(emailTemplatesRequest.Body, Constants.EmailTemplatePlaceholdersRequired[(EmailTemplateType)emailTemplatesRequest.TemplateType]);
        if (missingPlaceHolder != null) throw new AppException(string.Format(Constants.MISSING_PLACEHOLDER, missingPlaceHolder), 400);

        // Add
        if (emailTemplatesRequest.Id == 0)
        {
            // Check for if already exists for same type
            if (await emailTemplateRepository.Exists(x => x.TemplateType == emailTemplatesRequest.TemplateType && !x.IsDeleted)) throw new AppException(Constants.EMAIL_TEMPLATE_ALREDY_AVAILABLE_FOR_SAME_TYPE, 400);

            EmailTemplete emailTemplate = _mapper.Map<EmailTemplete>(emailTemplatesRequest);
            emailTemplate.Status = true;
            emailTemplate.CreatedBy = UserId ;
            emailTemplate.CreatedDate = DateTime.UtcNow;
            emailTemplate.IsDeleted = false;
            await emailTemplateRepository.AddAsync(emailTemplate);

            return Constants.EMAIL_TEMPLATE_ADDED;
        }

        // Update
        EmailTemplete existingTemplate = await emailTemplateRepository.GetAsync(x => x.Id == emailTemplatesRequest.Id && !x.IsDeleted) ?? throw new AppException(Constants.EMAIL_TEMPLATE_NOT_FOUND, 404);

        if (existingTemplate.TemplateType != emailTemplatesRequest.TemplateType)
        {
            // Check for if already exists for same type
            if (await emailTemplateRepository.Exists(x => x.TemplateType == emailTemplatesRequest.TemplateType && !x.IsDeleted)) throw new AppException(Constants.EMAIL_TEMPLATE_ALREDY_AVAILABLE_FOR_SAME_TYPE, 400);
        }
        existingTemplate = _mapper.Map(emailTemplatesRequest, existingTemplate);
        if(emailTemplatesRequest.Status == null)
        {
            throw new AppException(Constants.STATUS_REQUIRED, 400);
        }
        existingTemplate.Status = (bool)emailTemplatesRequest.Status; 
        existingTemplate.ModifiedBy = UserId;
        existingTemplate.ModifiedDate = DateTime.UtcNow;
        await emailTemplateRepository.UpdateAsync(existingTemplate);

        return Constants.EMAIL_TEMPLATE_UPDATED;
    }

    // HTML template broken or not
    private bool IsValidHtmlTemplate(string html)
    {
        var sanitizer = new HtmlSanitizer();
        var sanitized = sanitizer.Sanitize(html);

        return sanitized == html;
    }

    // Check if the required token exists in the HTML
    private string? GetMissingPlaceholder(string html, string[] requiredTokens)
    {
        foreach (string token in requiredTokens)
        {
            if (!html.Contains(token, StringComparison.OrdinalIgnoreCase))
                return token;
        }
        return null;
    }

    public async Task<EmailTemplatesResponseDto> GetEmailTemplateById(int id)
    {
        EmailTemplete emailTemplate = await emailTemplateRepository.GetAsync(x => x.Id == id && !x.IsDeleted) ?? throw new AppException(Constants.EMAIL_TEMPLATE_NOT_FOUND, 404);
        return _mapper.Map<EmailTemplatesResponseDto>(emailTemplate);
    }

    public async Task<string> UpdateEmailTemplateByAction(EmailTemplateActionRequestDTO emailTemplateActionRequest)
    {
        EmailTemplete emailTemplate = await emailTemplateRepository.GetAsync(x => x.Id == emailTemplateActionRequest.Id && !x.IsDeleted) ?? throw new AppException(Constants.EMAIL_TEMPLATE_NOT_FOUND, 404);

        // Message as per action
        string resultMessage = emailTemplateActionRequest.Action switch
        {
            EmailTemplateActionType.Delete => Constants.EMAIL_TEMPLATE_DELETE,
            EmailTemplateActionType.ChangeStatus => Constants.EMAIL_TEMPLATE_STATUS_UPDATED,
            _ => throw new AppException(Constants.INVALID_ACTION, 400)
        };

        switch (emailTemplateActionRequest.Action)
        {
            case EmailTemplateActionType.Delete:
                emailTemplate.IsDeleted = true;
                break;

            case EmailTemplateActionType.ChangeStatus:
                emailTemplate.Status = !emailTemplate.Status;
                break;
        }

        emailTemplate.ModifiedBy = UserId;
        emailTemplate.ModifiedDate = DateTime.UtcNow;

        await emailTemplateRepository.UpdateAsync(emailTemplate);

        return resultMessage;
    }
}