using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Exceptions;
using JobBoard.Domain.Helpers;
using JobBoard.Monolith.Contracts.Jobs;

namespace JobBoard.Domain.Entities.Users;

public class UserProfile : BaseAuditableEntity
{
    protected UserProfile()
    {
    }

    public int UserId { get; private set; }
    public User User { get; private set; } = null!;

    public string? Phone { get; private set; }
    public string? LinkedIn { get; private set; }
    public string? Portfolio { get; private set; }
    public string? Experience { get; private set; }
    public string? Skills { get; private set; }
    public string? PreferredLocation { get; private set; }
    public JobType? PreferredJobType { get; private set; }

    internal void SetUser(int userId) => UserId = userId;

    public void SetPhone(string? phone) => Phone = phone?.Trim();
    public void SetLinkedIn(string? linkedIn) => LinkedIn = linkedIn?.Trim();
    public void SetPortfolio(string? portfolio) => Portfolio = portfolio?.Trim();
    public void SetExperience(string? experience) => Experience = experience?.Trim();
    public void SetSkills(string? skills) => Skills = skills?.Trim();
    public void SetPreferredLocation(string? location) => PreferredLocation = location?.Trim();
    public void SetPreferredJobType(JobType? jobType) => PreferredJobType = jobType;

    public static UserProfile Create(UserProfileInput input)
    {
        var errors = new List<Error>();

        DomainGuard.AgainstInvalidId(input.UserId, "UserProfile.InvalidUserId", errors);

        if (errors.Count > 0)
            throw new DomainException("UserProfile.InvalidEntity", errors);

        var profile = new UserProfile
        {
            Phone = input.Phone?.Trim(),
            LinkedIn = input.LinkedIn?.Trim(),
            Portfolio = input.Portfolio?.Trim(),
            Experience = input.Experience?.Trim(),
            Skills = input.Skills != null ? string.Join(",", input.Skills) : null,
            PreferredLocation = input.PreferredLocation?.Trim(),
            PreferredJobType = input.PreferredJobType
        };

        profile.SetUser(input.UserId);
        profile.InternalId = input.InternalId;
        profile.Id = input.UId;

        EntityFactory.ApplyAudit(profile, input.CreatedAt, input.CreatedBy);

        return profile;
    }
}
