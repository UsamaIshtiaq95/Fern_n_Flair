namespace Application.DTO;

public class RoomCreateDto
{
    public string RoomName { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal? Area { get; set; }
    public string Unit { get; set; } = "ft";
    public string RoomType { get; set; } = "HomeSingle";
    public string? CeilingType { get; set; }
    public Guid? RoomGroupId { get; set; }
}

public class RoomUpdateDto
{
    public string RoomName { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal? Area { get; set; }
    public string Unit { get; set; } = "ft";
    public string? CeilingType { get; set; }
}

public class RoomResponseDto
{
    public int RoomId { get; set; }
    public string RoomName { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal? Area { get; set; }
    public string Unit { get; set; }
    public string RoomType { get; set; }
    public string? CeilingType { get; set; }
    public Guid? RoomGroupId { get; set; }
    public DateTime CreatedAt { get; set; }
}
