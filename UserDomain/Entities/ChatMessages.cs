
using System.ComponentModel.DataAnnotations;

namespace UserDomain.Entities;

public class ChatMessages
{
    [Key]
    public int MessageId { get; set; }

    [Required]
    public int ChatId { get; set; }
    public Chats Chat { get; set; }

    [Required, MaxLength(20)]
    public string Sender { get; set; }

    [Required]
    public string MessageText { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; } = false;
}
