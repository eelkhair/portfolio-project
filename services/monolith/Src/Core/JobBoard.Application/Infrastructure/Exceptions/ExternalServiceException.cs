using System.Net;

namespace JobBoard.Application.Infrastructure.Exceptions;

public sealed class ExternalServiceException : Exception
{
    public string Service { get; }
    public string Operation { get; }
    public HttpStatusCode StatusCode { get; }

    private ExternalServiceException(){}
    public ExternalServiceException(
        string service,
        string operation,
        HttpStatusCode statusCode,
        string responseBody)
        : base($"{service} {operation} failed with {statusCode}: {responseBody}")
    {
        Service = service;
        Operation = operation;
        StatusCode = statusCode;
    }
}