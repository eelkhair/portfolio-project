// ReSharper disable UnusedMember.Global

// ReSharper disable UnusedAutoPropertyAccessor.Global

using JobBoard.Domain.Exceptions;
using JobBoard.Domain.ValueObjects;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace JobBoard.Domain.Entities.Users;

public class User : BaseAuditableEntity
{
    public string FirstName { get;private  set; } 
    public string LastName { get; private set; }
    public string Email { get; private  set; }
    public string ExternalId { get; private  set; }
    private User(string firstName, string lastName, string email, string externalId)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        ExternalId = externalId;
    }
    
    public void SetFirstName(string firstName)
    {
        var result = UserFirstName.Create(firstName);
        
        if (result.IsFailure)
        {
            throw new DomainException("User.InvalidFirstName", result.Errors);
        }
        FirstName = result.Value!.Value;
    }
    
    public void SetLastName(string lastName)
    {
        var result = UserLastName.Create(lastName);
        if (result.IsFailure)
        {
            throw new DomainException("User.InvalidLastName", result.Errors);
        }
        LastName = result.Value!.Value;
    }
    
    public void SetEmail(string email)
    {
        var result = UserEmail.Create(email);
        if (result.IsFailure)
        {
            throw new DomainException("User.InvalidEmail", result.Errors);
        }
        
        Email = result.Value!.Value;
    }
    
    public void SetExternalId(string externalId)
    {
        var result = UserExternalId.Create(externalId);
        if (result.IsFailure)
        {
            throw new DomainException("User.InvalidExternalId", result.Errors);
        }
        
        ExternalId = result.Value!.Value;
    }
    
    public static User Create(string firstName, 
        string lastName, 
        string email, 
        string externalId,
        DateTime? createdAt = null,
        string? createdBy = null

        )
    {
         var user = ValidateAndCreateEntity(firstName, lastName, email, externalId);

         if (createdAt.HasValue)
         {
             user.CreatedAt = createdAt.Value; 
             user.UpdatedAt = createdAt.Value;
         }

         if (string.IsNullOrEmpty(createdBy)) return user;
         user.CreatedBy = createdBy;
         user.UpdatedBy = createdBy;

         return user;
    }
    
    private static User ValidateAndCreateEntity(string firstName, string lastName, string email, string externalId)
    {
        var errors = new List<Error>();
        
        var firstNameResult = UserFirstName.Create(firstName);
        if (firstNameResult.IsFailure)
        {
            errors.AddRange(firstNameResult.Errors);
        }
        
        var lastNameResult = UserLastName.Create(lastName);
        if (lastNameResult.IsFailure)
        {
            errors.AddRange(lastNameResult.Errors);
        }
        
        var emailResult = UserEmail.Create(email);
        if (emailResult.IsFailure)
        {
            errors.AddRange(emailResult.Errors);
        }
        
        var externalIdResult = UserExternalId.Create(externalId);
        if (externalIdResult.IsFailure)
        {
            errors.AddRange(externalIdResult.Errors);
        }
        
        if (errors.Count > 0)
        {
            throw new DomainException("User.InvalidEntity", errors);
        }
        
        return new  User(firstNameResult.Value!.Value, 
            lastNameResult.Value!.Value, emailResult.Value!.Value, externalIdResult.Value!.Value);
    }
}