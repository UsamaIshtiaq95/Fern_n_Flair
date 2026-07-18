using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UserDomain.Entities;
using UserDomain.Interface;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly SymmetricSecurityKey _key;

    public TokenService(IConfiguration config)
    {
        _config = config;
        var secret = _config["Jwt:Secret"] ??
            throw new ArgumentNullException("Jwt:Secret is missing in configuration");
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    public string CreateToken(Users user, out string jwtId)
    {
        jwtId = Guid.NewGuid().ToString();
        try
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.Name),
                new Claim(JwtRegisteredClaimNames.Jti, jwtId)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes()),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    _key,
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Token generation failed", ex);
        }
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public int GetJwtExpirationMinutes()
    {
        var expirationConfig = _config["Jwt:ExpirationInMinutes"];
        if (string.IsNullOrWhiteSpace(expirationConfig))
            return 20;

        if (!int.TryParse(expirationConfig, out var minutes) || minutes <= 0)
            return 20;

        return minutes;
    }

    public int GetRefreshTokenExpirationDays()
    {
        var config = _config["Jwt:RefreshTokenExpirationDays"];
        if (string.IsNullOrWhiteSpace(config))
            return 7;

        if (!int.TryParse(config, out var days) || days <= 0)
            return 7;

        return days;
    }
}
