using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserDomain.Entities;

public class RefreshToken
{
    [Key]
    public int TokenId { get; set; }

    [Required, MaxLength(500)]
    public string Token { get; set; }

    [Required, MaxLength(100)]
    public string JwtId { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public Users User { get; set; }

    public bool IsUsed { get; set; } = false;

    public bool IsRevoked { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }
}
