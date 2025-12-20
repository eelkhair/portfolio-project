using System.Net;

namespace JobBoard.Application.Infrastructure.Exceptions;

public sealed class ExternalServiceException(
    string service,
    string operation,
    HttpStatusCode statusCode,
    string responseBody)
    : Exception($"{service} {operation} failed with {statusCode}: {responseBody}")
{
    public string Service { get; } = service;
    public string Operation { get; } = operation;
    public HttpStatusCode StatusCode { get; } = statusCode;
}