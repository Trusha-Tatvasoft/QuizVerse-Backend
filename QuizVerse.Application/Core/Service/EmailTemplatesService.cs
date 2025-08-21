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

namespace QuizVerse.Application.Core.Service;

public class EmailTemplatesService(IGenericRepository<EmailTemplete> emailTemplateRepository, IMapper _mapper) : IEmailTemplatesService
{
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
                    string.Format(Constants.INVALID_COLUMN_NAME, pageListRequest.SortColumn),400);

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
}