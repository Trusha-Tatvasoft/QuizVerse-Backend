namespace QuizVerse.Application.Core.Interface;

public interface IGroqService
{
    Task<(string ContentString, int AILogId)> GenerateQuesions(string prompt);
}