namespace IdentityService.Domain.Exceptions;

public class DuplicateEntityException : DomainException
{
    public DuplicateEntityException(string entityName, string field, string value)
        : base($"A {entityName} with {field} '{value}' already exists.") { }

    public DuplicateEntityException(string message) : base(message) { }
}
