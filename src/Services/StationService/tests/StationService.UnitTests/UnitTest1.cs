using FluentAssertions;
using Moq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using StationService.Application.Commands;
using StationService.Application.Queries;
using StationService.Application.DTOs;
using StationService.Application.Interfaces;
using StationService.Domain.Entities;
using StationService.Domain.Events;
using StationService.Domain.Exceptions;
using StationService.Domain.Interfaces;
using StationService.Application.Common;

namespace StationService.UnitTests;

#region StationCommandTests

[TestFixture]
public class StationCommandTests
{
    private Mock<IStationRepository> _stationRepo = null!;
    private Mock<IRabbitMqPublisher> _publisher = null!;
    private Mock<IMapper> _mapper = null!;
    
    [SetUp]
    public void SetUp()
    {
        _stationRepo = new Mock<IStationRepository>();
        _publisher = new Mock<IRabbitMqPublisher>();
        _mapper = new Mock<IMapper>();
        
        _mapper.Setup(m => m.Map<StationDto>(It.IsAny<Station>()))
            .Returns((Station s) => new StationDto { Id = s.Id, StationCode = s.StationCode, StationName = s.StationName });
    }

    [Test]
    public async Task CreateStation_Valid_ReturnsStationDto()
    {
        var logger = new Mock<ILogger<CreateStationHandler>>();
        var handler = new CreateStationHandler(_stationRepo.Object, _publisher.Object, _mapper.Object, logger.Object);

        _stationRepo.Setup(r => r.ExistsByCodeAsync("CODE1", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _stationRepo.Setup(r => r.ExistsByLicenseAsync("LIC1", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _stationRepo.Setup(r => r.AddAsync(It.IsAny<Station>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Station s, CancellationToken _) => s);

        var cmd = new CreateStationCommand("CODE1", "Station 1", Guid.NewGuid(), "Addr 1", "City", "State", "111111", 10.0m, 20.0m, "LIC1", "05:00", "23:00", false);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.StationCode.Should().Be("CODE1");
        _publisher.Verify(p => p.PublishAsync(It.IsAny<StationCreatedEvent>(), "station.created", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateStation_DuplicateCode_ThrowsDuplicateEntityException()
    {
        var logger = new Mock<ILogger<CreateStationHandler>>();
        var handler = new CreateStationHandler(_stationRepo.Object, _publisher.Object, _mapper.Object, logger.Object);

        _stationRepo.Setup(r => r.ExistsByCodeAsync("CODE1", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var cmd = new CreateStationCommand("CODE1", "Station 1", Guid.NewGuid(), "Addr 1", "City", "State", "111111", 10.0m, 20.0m, "LIC1", "05:00", "23:00", false);
        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateEntityException>();
    }

    [Test]
    public async Task UpdateStation_Valid_UpdatesStation()
    {
        var logger = new Mock<ILogger<UpdateStationHandler>>();
        var handler = new UpdateStationHandler(_stationRepo.Object, _publisher.Object, _mapper.Object, logger.Object);

        var stationId = Guid.NewGuid();
        var existingStation = new Station { Id = stationId, StationCode = "CODE1", StationName = "Old Name" };
        _stationRepo.Setup(r => r.GetByIdAsync(stationId, It.IsAny<CancellationToken>())).ReturnsAsync(existingStation);

        var cmd = new UpdateStationCommand(stationId, "New Name", null, null, null, null, null, null, null, null, null);
        var result = await handler.Handle(cmd, CancellationToken.None);

        existingStation.StationName.Should().Be("New Name");
        _publisher.Verify(p => p.PublishAsync(It.IsAny<StationUpdatedEvent>(), "station.updated", It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Test]
    public async Task DeactivateStation_Valid_SetsIsActiveToFalse()
    {
        var logger = new Mock<ILogger<DeactivateStationHandler>>();
        var handler = new DeactivateStationHandler(_stationRepo.Object, _publisher.Object, logger.Object);

        var stationId = Guid.NewGuid();
        var existingStation = new Station { Id = stationId, StationCode = "CODE1", IsActive = true };
        _stationRepo.Setup(r => r.GetByIdAsync(stationId, It.IsAny<CancellationToken>())).ReturnsAsync(existingStation);

        var cmd = new DeactivateStationCommand(stationId, Guid.NewGuid());
        await handler.Handle(cmd, CancellationToken.None);

        existingStation.IsActive.Should().BeFalse();
        _publisher.Verify(p => p.PublishAsync(It.IsAny<StationDeactivatedEvent>(), "station.deactivated", It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region StationQueryTests

[TestFixture]
public class StationQueryTests
{
    private Mock<IStationRepository> _stationRepo = null!;
    private Mock<IMapper> _mapper = null!;
    
    [SetUp]
    public void SetUp()
    {
        _stationRepo = new Mock<IStationRepository>();
        _mapper = new Mock<IMapper>();
    }

    [Test]
    public async Task GetStationById_Found_ReturnsDto()
    {
        var stationId = Guid.NewGuid();
        _stationRepo.Setup(r => r.GetByIdAsync(stationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Station { Id = stationId });
        _mapper.Setup(m => m.Map<StationDto>(It.IsAny<Station>()))
            .Returns(new StationDto { Id = stationId });

        var handler = new GetStationByIdHandler(_stationRepo.Object, _mapper.Object);
        var result = await handler.Handle(new GetStationByIdQuery(stationId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(stationId);
    }

    [Test]
    public async Task GetNearbyStations_ReturnsSortedByDistance()
    {
        var stations = new List<Station>
        {
            new() { Id = Guid.NewGuid(), Latitude = 10.1m, Longitude = 20.1m },
            new() { Id = Guid.NewGuid(), Latitude = 10.05m, Longitude = 20.05m },
        };
        _stationRepo.Setup(r => r.GetNearbyAsync(10.0m, 20.0m, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stations);
            
        var dtos = new List<StationDto>
        {
            new() { Id = stations[0].Id, Latitude = 10.1m, Longitude = 20.1m },
            new() { Id = stations[1].Id, Latitude = 10.05m, Longitude = 20.05m }
        };
        _mapper.Setup(m => m.Map<IReadOnlyList<StationDto>>(It.IsAny<IReadOnlyList<Station>>())).Returns(dtos);

        var handler = new GetNearbyStationsHandler(_stationRepo.Object, _mapper.Object);
        var result = await handler.Handle(new GetNearbyStationsQuery(10.0m, 20.0m, 10, null), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(stations[1].Id); // closer station should be first
        result[1].Id.Should().Be(stations[0].Id);
    }
}

#endregion

#region FuelTypeTests

[TestFixture]
public class FuelTypeTests
{
    private Mock<IFuelTypeRepository> _fuelTypeRepo = null!;
    
    [SetUp]
    public void SetUp()
    {
        _fuelTypeRepo = new Mock<IFuelTypeRepository>();
    }

    [Test]
    public async Task CreateFuelType_Valid_ReturnsDto()
    {
        var logger = new Mock<ILogger<CreateFuelTypeHandler>>();
        var handler = new CreateFuelTypeHandler(_fuelTypeRepo.Object, logger.Object);
        
        _fuelTypeRepo.Setup(r => r.ExistsByNameAsync("Petrol", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _fuelTypeRepo.Setup(r => r.AddAsync(It.IsAny<FuelType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FuelType f, CancellationToken _) => f);

        var result = await handler.Handle(new CreateFuelTypeCommand("Petrol", "Description"), CancellationToken.None);
        
        result.Should().NotBeNull();
        result.Name.Should().Be("Petrol");
    }

    [Test]
    public async Task CreateFuelType_Duplicate_ThrowsDuplicateEntityException()
    {
        var logger = new Mock<ILogger<CreateFuelTypeHandler>>();
        var handler = new CreateFuelTypeHandler(_fuelTypeRepo.Object, logger.Object);
        
        _fuelTypeRepo.Setup(r => r.ExistsByNameAsync("Petrol", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => handler.Handle(new CreateFuelTypeCommand("Petrol", "Description"), CancellationToken.None);
        
        await act.Should().ThrowAsync<DuplicateEntityException>();
    }
}

#endregion
