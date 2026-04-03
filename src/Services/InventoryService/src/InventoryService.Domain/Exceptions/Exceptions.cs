namespace InventoryService.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entity, object key) : base($"{entity} with key '{key}' was not found.") { }
}

public class DuplicateEntityException : DomainException
{
    public DuplicateEntityException(string entity, string field, string value)
        : base($"A {entity} with {field} '{value}' already exists.") { }
}

public class InsufficientCapacityException : DomainException
{
    public InsufficientCapacityException(decimal current, decimal adding, decimal capacity)
        : base($"Cannot load {adding}L. Current stock: {current}L, capacity: {capacity}L. Would exceed by {current + adding - capacity}L.") { }
}

public class InsufficientStockException : DomainException
{
    public InsufficientStockException(Guid tankId, decimal requested, decimal available)
        : base($"Tank {tankId}: Insufficient stock. Requested: {requested}L, available: {available}L.") { }
}
