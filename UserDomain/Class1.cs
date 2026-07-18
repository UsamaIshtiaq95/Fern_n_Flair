namespace UserDomain;

public class MessageDto
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public List<ImageContent>? Images { get; set; }
}

public class ImageContent
{
    public string FileName { get; set; }
    public string Base64Data { get; set; }
    public string MediaType { get; set; }
}
