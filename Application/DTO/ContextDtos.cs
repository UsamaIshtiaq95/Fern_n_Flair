namespace Application.DTO;

public class ContextCreateDto
{
    public int RoomCount { get; set; }
    public string ContextData { get; set; }
    public string SourceAI { get; set; }
    public string Type { get; set; } = "home-single";
}

public class ContextUpdateDto
{
    public int RoomCount { get; set; }
    public string ContextData { get; set; }
    public string SourceAI { get; set; }
    public string Type { get; set; } = "home-single";
}

public class ContextResponseDto
{
    public int ContextId { get; set; }
    public int RoomCount { get; set; }
    public string ContextData { get; set; }
    public string SourceAI { get; set; }
    public string Type { get; set; }
    public DateTime CreatedAt { get; set; }
}
