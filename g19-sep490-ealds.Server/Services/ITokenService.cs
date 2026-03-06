using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user, IList<string> roles);
    string GenerateRefreshToken();
}
