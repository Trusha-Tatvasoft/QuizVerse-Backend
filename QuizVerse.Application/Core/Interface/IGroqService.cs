namespace QuizVerse.Application.Core.Interface;

public interface IGroqService
{
    Task<string> GenerateQuesions(string prompt);
}