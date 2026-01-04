using FastEndpoints;
using UserAPI.Contracts.Models.Requests;
using UserAPI.Contracts.Models.Responses;
using UserApi.Infrastructure.Data.Entities;

namespace UserApi.Features.Users.Mappers;

public class UserMapper: Mapper<CreateUserRequest, UserResponse, User>
{
    

    public override UserResponse FromEntity(User e) => new()
    {
        Email = e.Email,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        UId = e.UId,
       
    };
}