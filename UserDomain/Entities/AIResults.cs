
using System.ComponentModel.DataAnnotations;
using UserDomain.Entities;

namespace UserDomain.Entities;

public class AIResults
{
    [Key]
    public int ResultId { get; set; }

    [Required]
    public int ChatId { get; set; }
    public Chats Chat { get; set; }

    public int MessageId { get; set; }
    public ChatMessages Message { get; set; }

    public string AIResponse { get; set; }

    [MaxLength(50)]
    public string AIUsed { get; set; }

    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int LatencyMs { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

