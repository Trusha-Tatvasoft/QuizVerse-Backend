using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class AiProcessLog
{
    public int Id { get; set; }

    public int ModelName { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool IsSuccess { get; set; }

    public int Purpose { get; set; }

    public string? ExtraInfo { get; set; }
}
