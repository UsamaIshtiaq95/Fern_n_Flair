
using UserDomain.Entities;

namespace UserDomain.Interface;
public interface ITokenService
{
    string CreateToken(Users user, out string jwtId);
    string GenerateRefreshToken();
    int GetJwtExpirationMinutes();
    int GetRefreshTokenExpirationDays();
}