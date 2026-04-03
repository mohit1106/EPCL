namespace StationService.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.") { }
    public NotFoundException(string message) : base(message) { }
}

public class DuplicateEntityException : DomainException
{
    public DuplicateEntityException(string entityName, string field, string value)
        : base($"A {entityName} with {field} '{value}' already exists.") { }
    public DuplicateEntityException(string message) : base(message) { }
}
