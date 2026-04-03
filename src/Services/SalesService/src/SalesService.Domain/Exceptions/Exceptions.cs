namespace SalesService.Domain.Exceptions;

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

public class PumpNotActiveException : DomainException
{
    public PumpNotActiveException(Guid pumpId, string status)
        : base($"Pump {pumpId} is not active. Current status: {status}. Sales can only be recorded on active pumps.") { }
}

public class InvalidVehicleNumberException : DomainException
{
    public InvalidVehicleNumberException(string number)
        : base($"Vehicle number '{number}' does not match Indian RTO format (e.g., MH12AB1234).") { }
}

public class InsufficientWalletBalanceException : DomainException
{
    public InsufficientWalletBalanceException(decimal balance, decimal required)
        : base($"Insufficient wallet balance. Available: ₹{balance:F2}, Required: ₹{required:F2}.") { }
}
