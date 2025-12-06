using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Exceptions;
using JobBoard.Domain.Helpers;
using JobBoard.Domain.ValueObjects.User;

namespace JobBoard.Domain.Entities.Users;

public class User : BaseAuditableEntity
{
    protected User()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        ExternalId = string.Empty;
    }
    
    private User(string firstName, string lastName, string email, string? externalId)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        ExternalId = externalId;
    }
    
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string? ExternalId { get; private set; }
    
    public void SetFirstName(string firstName) =>
        FirstName = UserFirstName
            .Create(firstName)
            .Ensure<UserFirstName, string>("User.InvalidFirstName")!;

    public void SetLastName(string lastName) =>
        LastName = UserLastName
            .Create(lastName)
            .Ensure<UserLastName, string>("User.InvalidLastName")!;

    public void SetEmail(string email) =>
        Email = UserEmail
            .Create(email)
            .Ensure<UserEmail, string>("User.InvalidEmail")!;

    public void SetExternalId(string externalId) =>
        ExternalId = UserExternalId
            .Create(externalId)
            .Ensure<UserExternalId, string?>("User.InvalidExternalId")!;
    
    public static User Create(
        string firstName,
        string lastName,
        string email,
        string? externalId,
        Guid id,
        int internalId,
        DateTime? createdAt = null,
        string? createdBy = null)
    {
        var user = ValidateAndCreate(firstName, lastName, email, externalId);
        user.InternalId = internalId;
        user.Id = id;
        EntityFactory.ApplyAudit(user, createdAt, createdBy);

        return user;
    }
    
    private static User ValidateAndCreate(
        string firstName, 
        string lastName, 
        string email, 
        string? externalId)
    {
        var errors = new List<Error>();

        var fName = UserFirstName.Create(firstName).Collect<UserFirstName, string>(errors)!;
        var lName = UserLastName.Create(lastName).Collect<UserLastName, string>(errors)!;
        var mail = UserEmail.Create(email).Collect<UserEmail, string>(errors)!;
        var extId = UserExternalId.Create(externalId).Collect<UserExternalId, string?>(errors)!;

        if (errors.Count > 0)
            throw new DomainException("User.InvalidEntity", errors);

        return new User(fName, lName, mail, extId);
    }
}
