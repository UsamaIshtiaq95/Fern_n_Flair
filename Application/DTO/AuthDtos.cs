namespace Application.DTO;

public class RefreshTokenRequestDto
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
}

public class RefreshTokenResponseDto
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public string Message { get; set; }
}
