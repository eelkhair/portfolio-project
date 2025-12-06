using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Application.Interfaces.Users;
using JobBoard.Domain.Entities.Users;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace JobBoard.Application.Infrastructure.UserSync;

public class UserSyncService(
    IUserRepository userRepository,
    IUserAccessor userAccessor,
    IUnitOfWork unitOfWork
) : IUserSyncService
{
    public async Task EnsureUserExistsAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindUserByExternalIdOrIdAsync(
            userId,
            cancellationToken
        );
        
        if (user == null)
        {
            var (id, uid) = await unitOfWork.GetNextValueFromSequenceAsync(typeof(User), cancellationToken);
            var entity = User.Create(
                firstName: userAccessor.FirstName!,
                lastName: userAccessor.LastName!,
                email: userAccessor.Email!,
                externalId: userId,
                id: uid, internalId: id,
                createdAt:DateTime.UtcNow,
                createdBy:userId
            );
            
            await userRepository.AddAsync(entity, cancellationToken);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(userAccessor.FirstName) && !string.IsNullOrWhiteSpace(userAccessor.LastName) && !string.IsNullOrWhiteSpace(userAccessor.Email) 
                && (!user.FirstName.Equals(userAccessor.FirstName, StringComparison.InvariantCultureIgnoreCase) ||
                    !user.LastName.Equals(userAccessor.LastName, StringComparison.InvariantCultureIgnoreCase) ||
                    !user.Email.Equals(userAccessor.Email, StringComparison.InvariantCultureIgnoreCase)))
            {
                user.SetFirstName(userAccessor.FirstName!);
                user.SetLastName(userAccessor.LastName!);
                user.SetEmail(userAccessor.Email!);
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = userId;
            }
        }

        await unitOfWork.SaveChangesAsync(userId, cancellationToken);

    }
}