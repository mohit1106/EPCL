using MediatR;
using Microsoft.Extensions.Logging;
using AuditService.Application.DTOs;
using AuditService.Domain.Entities;
using AuditService.Domain.Interfaces;

namespace AuditService.Application.Commands;

/// <summary>
/// AppendAuditLog — the ONLY write operation allowed on the audit table.
/// No update, no delete, no modify — append only, by design.
/// </summary>
public record AppendAuditLogCommand(
    Guid EventId, string EntityType, Guid EntityId, string Operation,
    string? OldValues, string? NewValues,
    Guid? ChangedByUserId, string? ChangedByRole,
    string? IpAddress, string? CorrelationId,
    string ServiceName, DateTimeOffset Timestamp) : IRequest<AuditLogDto>;

public class AppendAuditLogHandler(IAuditLogRepository repo, ILogger<AppendAuditLogHandler> logger)
    : IRequestHandler<AppendAuditLogCommand, AuditLogDto>
{
    public async Task<AuditLogDto> Handle(AppendAuditLogCommand cmd, CancellationToken ct)
    {
        // Idempotency: skip if this event was already logged
        if (await repo.EventAlreadyLoggedAsync(cmd.EventId, ct))
        {
            logger.LogInformation("Audit event {EventId} already logged, skipping", cmd.EventId);
            return new AuditLogDto(Guid.Empty, cmd.EventId, cmd.EntityType, cmd.EntityId,
                cmd.Operation, null, null, null, null, null, null, cmd.ServiceName, cmd.Timestamp);
        }

        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            EventId = cmd.EventId,
            EntityType = cmd.EntityType,
            EntityId = cmd.EntityId,
            Operation = cmd.Operation,
            OldValues = cmd.OldValues,
            NewValues = cmd.NewValues,
            ChangedByUserId = cmd.ChangedByUserId,
            ChangedByRole = cmd.ChangedByRole,
            IpAddress = cmd.IpAddress,
            CorrelationId = cmd.CorrelationId,
            ServiceName = cmd.ServiceName,
            Timestamp = cmd.Timestamp
        };

        await repo.AppendAsync(log, ct);
        logger.LogInformation("Audit log appended: {EntityType}.{Operation} by {Service}",
            cmd.EntityType, cmd.Operation, cmd.ServiceName);
        return MapToDto(log);
    }

    private static AuditLogDto MapToDto(AuditLog l) => new(
        l.Id, l.EventId, l.EntityType, l.EntityId, l.Operation,
        l.OldValues, l.NewValues, l.ChangedByUserId, l.ChangedByRole,
        l.IpAddress, l.CorrelationId, l.ServiceName, l.Timestamp);
}
