using MediatR;
using FluentValidation;
using AutoMapper;
using Microsoft.Extensions.Logging;
using StationService.Application.DTOs;
using StationService.Application.Interfaces;
using StationService.Domain.Entities;
using StationService.Domain.Events;
using StationService.Domain.Exceptions;
using StationService.Domain.Interfaces;

namespace StationService.Application.Commands;

// ── CreateStation ──────────────────────────────────────────────────
public record CreateStationCommand(
    string StationCode, string StationName, Guid DealerUserId,
    string AddressLine1, string City, string State, string PinCode,
    decimal Latitude, decimal Longitude, string LicenseNumber,
    string OperatingHoursStart, string OperatingHoursEnd, bool Is24x7
) : IRequest<StationDto>;

public class CreateStationValidator : AbstractValidator<CreateStationCommand>
{
    public CreateStationValidator()
    {
        RuleFor(x => x.StationCode).NotEmpty().MaximumLength(15);
        RuleFor(x => x.StationName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DealerUserId).NotEmpty();
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(250);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PinCode).NotEmpty().Matches(@"^\d{6}$").WithMessage("PinCode must be 6 digits.");
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(60);
        RuleFor(x => x.OperatingHoursStart).NotEmpty().Matches(@"^\d{2}:\d{2}$");
        RuleFor(x => x.OperatingHoursEnd).NotEmpty().Matches(@"^\d{2}:\d{2}$");
    }
}

public class CreateStationHandler(
    IStationRepository stationRepo,
    IRabbitMqPublisher publisher,
    IMapper mapper,
    ILogger<CreateStationHandler> logger)
    : IRequestHandler<CreateStationCommand, StationDto>
{
    public async Task<StationDto> Handle(CreateStationCommand cmd, CancellationToken ct)
    {
        if (await stationRepo.ExistsByCodeAsync(cmd.StationCode, ct))
            throw new DuplicateEntityException("Station", "code", cmd.StationCode);
        if (await stationRepo.ExistsByLicenseAsync(cmd.LicenseNumber, ct))
            throw new DuplicateEntityException("Station", "license number", cmd.LicenseNumber);

        var station = new Station
        {
            Id = Guid.NewGuid(),
            StationCode = cmd.StationCode.ToUpperInvariant(),
            StationName = cmd.StationName,
            DealerUserId = cmd.DealerUserId,
            AddressLine1 = cmd.AddressLine1,
            City = cmd.City,
            State = cmd.State,
            PinCode = cmd.PinCode,
            Latitude = cmd.Latitude,
            Longitude = cmd.Longitude,
            LicenseNumber = cmd.LicenseNumber,
            OperatingHoursStart = TimeOnly.Parse(cmd.OperatingHoursStart),
            OperatingHoursEnd = TimeOnly.Parse(cmd.OperatingHoursEnd),
            Is24x7 = cmd.Is24x7,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await stationRepo.AddAsync(station, ct);

        await publisher.PublishAsync(new StationCreatedEvent
        {
            EventType = nameof(StationCreatedEvent),
            StationId = station.Id,
            StationCode = station.StationCode,
            StationName = station.StationName,
            DealerUserId = station.DealerUserId
        }, "station.created", ct);

        logger.LogInformation("Station created. StationId: {StationId}, Code: {StationCode}",
            station.Id, station.StationCode);

        return mapper.Map<StationDto>(station);
    }
}

// ── UpdateStation ──────────────────────────────────────────────────
public record UpdateStationCommand(
    Guid StationId, string? StationName, string? AddressLine1,
    string? City, string? State, string? PinCode,
    decimal? Latitude, decimal? Longitude,
    string? OperatingHoursStart, string? OperatingHoursEnd, bool? Is24x7
) : IRequest<StationDto>;

public class UpdateStationHandler(
    IStationRepository stationRepo,
    IRabbitMqPublisher publisher,
    IMapper mapper,
    ILogger<UpdateStationHandler> logger)
    : IRequestHandler<UpdateStationCommand, StationDto>
{
    public async Task<StationDto> Handle(UpdateStationCommand cmd, CancellationToken ct)
    {
        var station = await stationRepo.GetByIdAsync(cmd.StationId, ct)
            ?? throw new NotFoundException("Station", cmd.StationId);

        if (cmd.StationName != null) station.StationName = cmd.StationName;
        if (cmd.AddressLine1 != null) station.AddressLine1 = cmd.AddressLine1;
        if (cmd.City != null) station.City = cmd.City;
        if (cmd.State != null) station.State = cmd.State;
        if (cmd.PinCode != null) station.PinCode = cmd.PinCode;
        if (cmd.Latitude.HasValue) station.Latitude = cmd.Latitude.Value;
        if (cmd.Longitude.HasValue) station.Longitude = cmd.Longitude.Value;
        if (cmd.OperatingHoursStart != null) station.OperatingHoursStart = TimeOnly.Parse(cmd.OperatingHoursStart);
        if (cmd.OperatingHoursEnd != null) station.OperatingHoursEnd = TimeOnly.Parse(cmd.OperatingHoursEnd);
        if (cmd.Is24x7.HasValue) station.Is24x7 = cmd.Is24x7.Value;

        station.UpdatedAt = DateTimeOffset.UtcNow;
        await stationRepo.UpdateAsync(station, ct);

        await publisher.PublishAsync(new StationUpdatedEvent
        {
            EventType = nameof(StationUpdatedEvent),
            StationId = station.Id,
            StationCode = station.StationCode
        }, "station.updated", ct);

        logger.LogInformation("Station updated. StationId: {StationId}", station.Id);
        return mapper.Map<StationDto>(station);
    }
}

// ── DeactivateStation ──────────────────────────────────────────────
public record DeactivateStationCommand(Guid StationId, Guid DeactivatedByUserId) : IRequest<MessageResponseDto>;

public class DeactivateStationHandler(
    IStationRepository stationRepo,
    IRabbitMqPublisher publisher,
    ILogger<DeactivateStationHandler> logger)
    : IRequestHandler<DeactivateStationCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(DeactivateStationCommand cmd, CancellationToken ct)
    {
        var station = await stationRepo.GetByIdAsync(cmd.StationId, ct)
            ?? throw new NotFoundException("Station", cmd.StationId);

        station.IsActive = false;
        station.UpdatedAt = DateTimeOffset.UtcNow;
        await stationRepo.UpdateAsync(station, ct);

        await publisher.PublishAsync(new StationDeactivatedEvent
        {
            EventType = nameof(StationDeactivatedEvent),
            StationId = station.Id,
            StationCode = station.StationCode,
            DeactivatedByUserId = cmd.DeactivatedByUserId
        }, "station.deactivated", ct);

        logger.LogInformation("Station deactivated. StationId: {StationId}", station.Id);
        return new MessageResponseDto($"Station {station.StationCode} has been deactivated.");
    }
}

// ── AssignDealer ───────────────────────────────────────────────────
public record AssignDealerCommand(Guid StationId, Guid DealerUserId) : IRequest<StationDto>;

public class AssignDealerHandler(
    IStationRepository stationRepo,
    IRabbitMqPublisher publisher,
    IMapper mapper,
    ILogger<AssignDealerHandler> logger)
    : IRequestHandler<AssignDealerCommand, StationDto>
{
    public async Task<StationDto> Handle(AssignDealerCommand cmd, CancellationToken ct)
    {
        var station = await stationRepo.GetByIdAsync(cmd.StationId, ct)
            ?? throw new NotFoundException("Station", cmd.StationId);

        station.DealerUserId = cmd.DealerUserId;
        station.UpdatedAt = DateTimeOffset.UtcNow;
        await stationRepo.UpdateAsync(station, ct);

        await publisher.PublishAsync(new StationUpdatedEvent
        {
            EventType = nameof(StationUpdatedEvent),
            StationId = station.Id,
            StationCode = station.StationCode
        }, "station.dealer.assigned", ct);

        logger.LogInformation("Dealer assigned to station. StationId: {StationId}, DealerUserId: {DealerUserId}",
            station.Id, cmd.DealerUserId);
        return mapper.Map<StationDto>(station);
    }
}

// ── CreateFuelType ─────────────────────────────────────────────────
public record CreateFuelTypeCommand(string Name, string? Description) : IRequest<FuelTypeDto>;

public class CreateFuelTypeValidator : AbstractValidator<CreateFuelTypeCommand>
{
    public CreateFuelTypeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(200);
    }
}

public class CreateFuelTypeHandler(
    IFuelTypeRepository fuelTypeRepo,
    ILogger<CreateFuelTypeHandler> logger)
    : IRequestHandler<CreateFuelTypeCommand, FuelTypeDto>
{
    public async Task<FuelTypeDto> Handle(CreateFuelTypeCommand cmd, CancellationToken ct)
    {
        if (await fuelTypeRepo.ExistsByNameAsync(cmd.Name, ct))
            throw new DuplicateEntityException("FuelType", "name", cmd.Name);

        var fuelType = new FuelType
        {
            Id = Guid.NewGuid(),
            Name = cmd.Name,
            Description = cmd.Description,
            IsActive = true
        };

        await fuelTypeRepo.AddAsync(fuelType, ct);
        logger.LogInformation("FuelType created. Id: {FuelTypeId}, Name: {Name}", fuelType.Id, fuelType.Name);

        return new FuelTypeDto { Id = fuelType.Id, Name = fuelType.Name, Description = fuelType.Description, IsActive = true };
    }
}

// ── UpdateFuelType ─────────────────────────────────────────────────
public record UpdateFuelTypeCommand(Guid FuelTypeId, string? Name, string? Description, bool? IsActive) : IRequest<FuelTypeDto>;

public class UpdateFuelTypeHandler(
    IFuelTypeRepository fuelTypeRepo,
    ILogger<UpdateFuelTypeHandler> logger)
    : IRequestHandler<UpdateFuelTypeCommand, FuelTypeDto>
{
    public async Task<FuelTypeDto> Handle(UpdateFuelTypeCommand cmd, CancellationToken ct)
    {
        var fuelType = await fuelTypeRepo.GetByIdAsync(cmd.FuelTypeId, ct)
            ?? throw new NotFoundException("FuelType", cmd.FuelTypeId);

        if (cmd.Name != null) fuelType.Name = cmd.Name;
        if (cmd.Description != null) fuelType.Description = cmd.Description;
        if (cmd.IsActive.HasValue) fuelType.IsActive = cmd.IsActive.Value;

        await fuelTypeRepo.UpdateAsync(fuelType, ct);
        logger.LogInformation("FuelType updated. Id: {FuelTypeId}", fuelType.Id);

        return new FuelTypeDto { Id = fuelType.Id, Name = fuelType.Name, Description = fuelType.Description, IsActive = fuelType.IsActive };
    }
}
