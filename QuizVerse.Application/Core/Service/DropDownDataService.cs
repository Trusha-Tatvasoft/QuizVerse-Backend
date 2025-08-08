using System.Linq.Expressions;
using System.Reflection;
using AutoMapper;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class DropDownDataService(
    IMapper _mapper,
    IMemoryCacheService _cacheService,
    IGenericRepository<QuizCategory> _quizCategoryRepository,
    IGenericRepository<QuizDifficulty> _quizDifficultyRepository,
    IGenericRepository<QuizTag> _quizTagRepository,
    IGenericRepository<QuestionDifficulty> _questionDifficultyRepository,
    IGenericRepository<QuestionType> _questionTypeRepository
) : IDropDownDataService
{
    public List<CommonListDropDownDto> GetDropDownListData(DropDownType dropDownType)
    {
        return _cacheService.GetOrSet($"{dropDownType}", () =>
        {
            var query = GetRepository(dropDownType);
            return ApplyDefaultFilters(query);
        });
    }

    private IQueryable<object> GetRepository(DropDownType dropDownType)
    {
        return dropDownType switch
        {
            DropDownType.QuizCategory => _quizCategoryRepository.GetQueryableInclude(),
            DropDownType.QuizDifficulty => _quizDifficultyRepository.GetQueryableInclude(),
            DropDownType.QuizTag => _quizTagRepository.GetQueryableInclude(),
            DropDownType.QuestionDifficulty => _questionDifficultyRepository.GetQueryableInclude(),
            DropDownType.QuestionType => _questionTypeRepository.GetQueryableInclude(),
            _ => throw new ArgumentOutOfRangeException(nameof(dropDownType), dropDownType, null)
        };
    }

    private List<CommonListDropDownDto> ApplyDefaultFilters(IQueryable<object> query)
    {
        // Dynamically build expression trees if possible
        Type? entityType = query.ElementType;

        ParameterExpression? parameter = Expression.Parameter(entityType, "x");

        // IsDeleted filter
        PropertyInfo? isDeletedProp = entityType.GetProperty(Constants.IS_DELETED);
        if (isDeletedProp is not null && isDeletedProp.PropertyType == typeof(bool))
        {
            MemberExpression? isDeletedAccess = Expression.Property(parameter, isDeletedProp);
            UnaryExpression? notIsDeleted = Expression.Not(isDeletedAccess);
            LambdaExpression? lambda = Expression.Lambda(notIsDeleted, parameter);

            MethodInfo? whereMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == Constants.WHERE && m.GetParameters().Length == 2)
                .MakeGenericMethod(entityType);

            query = (IQueryable<object>)whereMethod.Invoke(null, new object[] { query, lambda })!;
        }

        // OrderBy Id
        PropertyInfo? idProp = entityType.GetProperty(Constants.ID);
        if (idProp is not null && idProp.PropertyType == typeof(int))
        {
            MemberExpression? idAccess = Expression.Property(parameter, idProp);
            LambdaExpression? lambda = Expression.Lambda(idAccess, parameter);

            MethodInfo? orderByMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == Constants.ORDER_BY && m.GetParameters().Length == 2)
                .MakeGenericMethod(entityType, typeof(int));

            query = (IQueryable<object>)orderByMethod.Invoke(null, new object[] { query, lambda })!;
        }

        return _mapper.ProjectTo<CommonListDropDownDto>(query).ToList();
    }

    public void ClearCache(DropDownType dropDownType)
    {
        _cacheService.Clear($"{dropDownType}");
    }
}
