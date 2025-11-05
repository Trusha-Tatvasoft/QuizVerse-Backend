public class AiConfigurationCardDetailsDTO
{
    public int CurruntMonthApiCalls { get; set; } 
    public int GeneratedQuestionsCurruntMonth { get; set; }
    public int GeneratedQuestionsLastMonth { get; set; }
    public decimal SuccessRate { get; set; }
}

public class AiUsesDetailsDTO
{
    public int TodaysApiCalls { get; set; } 
    public decimal AverageResponseTimeInSecond { get; set; }
    public decimal ErrorRate { get; set; }
}