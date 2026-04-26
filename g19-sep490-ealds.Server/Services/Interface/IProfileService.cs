using g19_sep490_ealds.Server.DTOs.Profile;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IProfileService
{
    Task<UserProfileDto> GetProfileAsync(int userId);
    Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequestDto request);
    Task ChangePasswordAsync(int userId, ChangePasswordRequestDto request);
}
