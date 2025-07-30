using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class PasswordResetToken
{
    public int TokenId { get; set; }

    public int UserId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpireAt { get; set; }

    public bool? IsUsed { get; set; }

    public virtual User User { get; set; } = null!;
}
