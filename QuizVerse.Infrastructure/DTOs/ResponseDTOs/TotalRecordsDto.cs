using System.ComponentModel.DataAnnotations.Schema;
 
namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;
 
public class TotalRecordsDto
{
    [Column("total_records")]
    public int TotalRecords { get; set; }
}
 