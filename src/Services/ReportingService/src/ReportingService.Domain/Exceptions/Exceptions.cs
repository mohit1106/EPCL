namespace ReportingService.Domain.Exceptions;
public class DomainException(string message) : Exception(message);
public class NotFoundException(string entity, object key) : DomainException($"{entity} with key '{key}' was not found.");
