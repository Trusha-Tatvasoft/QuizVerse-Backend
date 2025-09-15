namespace QuizVerse.Infrastructure.DTOs
{
    public class QuizAnswerCheckDto
    {
        public string QuestionName { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public string GivenAnswer { get; set; } = string.Empty;
    }
}
