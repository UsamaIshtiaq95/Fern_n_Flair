
using System.ComponentModel.DataAnnotations;

namespace UserDomain.Entities;

public class Contexts
{
    [Key]
    public int ContextId { get; set; }

    public string ContextData { get; set; }

    [MaxLength(50)]
    public string SourceAI { get; set; }

    public int RoomCount { get; set; }

    [MaxLength(50)]
    public string Type { get; set; } = "home-single";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Chats> Chats { get; set; }
}
