using System.ComponentModel.DataAnnotations;
namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuizCategoryDTO
{
    public int? Id { get; set; }
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
    public string CategoryName { get; set; } = null!;
    [StringLength(200, ErrorMessage = "Icon path or name cannot exceed 200 characters.")]
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
    public string? Description { get; set; }
    public int QuizCount { get; set; }
    public DateTime CreatedDate { get; set; }
}
