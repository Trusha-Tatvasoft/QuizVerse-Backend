using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Application.Core.Interface;

public interface IDropDownDataService
{
    List<CommonListDropDownDto> GetDropDownListData(DropDownType dropDownType);
    void ClearCache(DropDownType dropDownType);
}