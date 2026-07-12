namespace Application.DTO;

public class RoomCreateDto
{
    public string RoomName { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public string Unit { get; set; } = "ft";
}

public class RoomUpdateDto
{
    public string RoomName { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public string Unit { get; set; } = "ft";
}

public class RoomResponseDto
{
    public int RoomId { get; set; }
    public string RoomName { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public string Unit { get; set; }
    public DateTime CreatedAt { get; set; }
}
