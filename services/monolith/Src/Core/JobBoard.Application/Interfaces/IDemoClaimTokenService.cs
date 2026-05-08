namespace JobBoard.Application.Interfaces;

public interface IDemoClaimTokenService
{
    string Issue(Guid companyUId, DateTime expiresAt);
    bool Verify(string token, Guid companyUId);
}
