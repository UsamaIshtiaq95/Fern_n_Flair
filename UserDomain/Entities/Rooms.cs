
using System.ComponentModel.DataAnnotations;

namespace UserDomain.Entities;

public class Rooms
{
    [Key]
    public int RoomId { get; set; }

    [Required, MaxLength(100)]
    public string RoomName { get; set; }

    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }

    [MaxLength(20)]
    public string Unit { get; set; } = "ft";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; } = false;

    public ICollection<Chats> Chats { get; set; }
    public ICollection<Images> Images { get; set; }
}
