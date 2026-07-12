
using System.ComponentModel.DataAnnotations;

namespace UserDomain.Entities;

public class Images
{
    [Key]
    public int ImageId { get; set; }

    public int? RoomId { get; set; }
    public Rooms Room { get; set; }

    public int? ChatId { get; set; }
    public Chats Chat { get; set; }

    public int? MessageId { get; set; }
    public ChatMessages Message { get; set; }

    [Required, MaxLength(255)]
    public string FileName { get; set; }

    [Required, MaxLength(500)]
    public string FilePath { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.Now;
}
