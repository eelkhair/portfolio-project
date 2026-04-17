using System.Net;

namespace JobBoard.Application.Interfaces.Infrastructure.Keycloak;

/// <summary>Thrown when Keycloak Admin API returns a non-success response or a domain conflict.</summary>
public class KeycloakOperationException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public KeycloakOperationException(string message, HttpStatusCode statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public KeycloakOperationException(string message, HttpStatusCode statusCode, Exception inner)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}
