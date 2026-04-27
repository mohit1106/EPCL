using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using AutoMapper;
using IdentityService.Application.Commands.RegisterUser;
using IdentityService.Application.Commands.LoginUser;
using IdentityService.Application.Commands.ChangePassword;
using IdentityService.Application.Commands.ForgotPassword;
using IdentityService.Application.Commands.LockUser;
using IdentityService.Application.Commands.UpdateUserRole;
using IdentityService.Application.Common;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Application.Queries.GetAllUsers;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Domain.Events;
using IdentityService.Domain.Exceptions;
using IdentityService.Domain.Interfaces;

namespace IdentityService.UnitTests;

#region RegisterUser Tests

[TestFixture]
public class RegisterUserHandlerTests
{
    private Mock<IUserRepository> _userRepo = null!;
    private Mock<IRabbitMqPublisher> _publisher = null!;
    private Mock<ILogger<RegisterUserCommandHandler>> _logger = null!;
    private RegisterUserCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = new Mock<IUserRepository>();
        _publisher = new Mock<IRabbitMqPublisher>();
        _logger = new Mock<ILogger<RegisterUserCommandHandler>>();
        _handler = new RegisterUserCommandHandler(_userRepo.Object, _publisher.Object, _logger.Object);
    }

    [Test]
    public async Task Handle_ValidRegistration_ReturnsSuccessResponse()
    {
        // Arrange
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.ExistsByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync((User u, CancellationToken _) => u);

        var cmd = new RegisterUserCommand("John Doe", "john@test.com", "+919876543210", "SecureP@ss1", "SecureP@ss1", "Customer");

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().NotBeEmpty();
        result.Message.Should().Contain("successful");
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(p => p.PublishAsync(It.IsAny<UserRegisteredEvent>(), "identity.user.registered", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_DuplicateEmail_ThrowsDuplicateEntityException()
    {
        _userRepo.Setup(r => r.ExistsByEmailAsync("john@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var cmd = new RegisterUserCommand("John", "john@test.com", "+919876543210", "P@ss1", "P@ss1", "Customer");

        var act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateEntityException>();
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_DuplicatePhone_ThrowsDuplicateEntityException()
    {
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.ExistsByPhoneAsync("+919876543210", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var cmd = new RegisterUserCommand("John", "john@test.com", "+919876543210", "P@ss1", "P@ss1", "Customer");

        var act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateEntityException>();
    }

    [Test]
    public async Task Handle_InvalidRole_ThrowsDomainException()
    {
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.ExistsByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var cmd = new RegisterUserCommand("John", "john@test.com", "+919876543210", "P@ss1", "P@ss1", "InvalidRole");

        var act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Invalid role*");
    }

    [Test]
    public async Task Handle_ValidRegistration_SetsEmailToLowercase()
    {
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.ExistsByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        User? savedUser = null;
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .Callback<User, CancellationToken>((u, _) => savedUser = u)
                 .ReturnsAsync((User u, CancellationToken _) => u);

        var cmd = new RegisterUserCommand("John", "JOHN@TEST.COM", "+919876543210", "P@ss1", "P@ss1", "Customer");
        await _handler.Handle(cmd, CancellationToken.None);

        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be("john@test.com");
    }
}

#endregion

#region LoginUser Tests

[TestFixture]
public class LoginUserHandlerTests
{
    private Mock<IUserRepository> _userRepo = null!;
    private Mock<IJwtService> _jwtService = null!;
    private Mock<IRabbitMqPublisher> _publisher = null!;
    private Mock<IMapper> _mapper = null!;
    private Mock<ILogger<LoginUserCommandHandler>> _logger = null!;
    private LoginUserCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = new Mock<IUserRepository>();
        _jwtService = new Mock<IJwtService>();
        _publisher = new Mock<IRabbitMqPublisher>();
        _mapper = new Mock<IMapper>();
        _logger = new Mock<ILogger<LoginUserCommandHandler>>();
        _handler = new LoginUserCommandHandler(_userRepo.Object, _jwtService.Object, _publisher.Object, _mapper.Object, _logger.Object);
    }

    private User CreateActiveUser(string email = "john@test.com", string password = "P@ss1")
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = email,
            PhoneNumber = "+919876543210",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
            Role = UserRole.Customer,
            IsActive = true,
            IsEmailVerified = true,
            AuthProvider = AuthProvider.Local,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    [Test]
    public async Task Handle_ValidCredentials_ReturnsLoginResponse()
    {
        var user = CreateActiveUser();
        _userRepo.Setup(r => r.GetByEmailAsync("john@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _jwtService.Setup(j => j.GenerateAccessToken(user)).Returns("jwt-token-123");
        _jwtService.Setup(j => j.GenerateRefreshTokenAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new RefreshToken { Token = "refresh-token", UserId = user.Id });
        _mapper.Setup(m => m.Map<UserDto>(user)).Returns(new UserDto { Id = user.Id, Email = user.Email });

        var cmd = new LoginUserCommand("john@test.com", "P@ss1");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("jwt-token-123");
        result.ExpiresIn.Should().Be(900);
    }

    [Test]
    public async Task Handle_UserNotFound_ThrowsInvalidCredentials()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var cmd = new LoginUserCommand("notfound@test.com", "P@ss1");
        var act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Test]
    public async Task Handle_InactiveUser_ThrowsDomainException()
    {
        var user = CreateActiveUser();
        user.IsActive = false;
        _userRepo.Setup(r => r.GetByEmailAsync("john@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var cmd = new LoginUserCommand("john@test.com", "P@ss1");
        var act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*deactivated*");
    }

    [Test]
    public async Task Handle_WrongPassword_IncrementsFailedAttempts()
    {
        var user = CreateActiveUser();
        _userRepo.Setup(r => r.GetByEmailAsync("john@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var cmd = new LoginUserCommand("john@test.com", "WrongPassword");
        var act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _userRepo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_LockedAccount_ThrowsAccountLockedException()
    {
        var user = CreateActiveUser();
        user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30);
        _userRepo.Setup(r => r.GetByEmailAsync("john@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var cmd = new LoginUserCommand("john@test.com", "P@ss1");
        var act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<AccountLockedException>();
    }
}

#endregion

#region ChangePassword Tests

[TestFixture]
public class ChangePasswordHandlerTests
{
    private Mock<IUserRepository> _userRepo = null!;
    private Mock<ILogger<ChangePasswordCommandHandler>> _logger = null!;
    private ChangePasswordCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = new Mock<IUserRepository>();
        _logger = new Mock<ILogger<ChangePasswordCommandHandler>>();
        _handler = new ChangePasswordCommandHandler(_userRepo.Object, _logger.Object);
    }

    [Test]
    public async Task Handle_ValidOldPassword_ChangesPassword()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldP@ss1", workFactor: 12),
            IsActive = true,
            Role = UserRole.Customer
        };
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var cmd = new ChangePasswordCommand(user.Id, "OldP@ss1", "NewP@ss1");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Message.Should().Contain("success");
        _userRepo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_WrongOldPassword_ThrowsDomainException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldP@ss1", workFactor: 12),
            IsActive = true,
        };
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var cmd = new ChangePasswordCommand(user.Id, "WrongOld", "NewP@ss1");
        var act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

#endregion

#region ForgotPassword Tests

[TestFixture]
public class ForgotPasswordHandlerTests
{
    private Mock<IUserRepository> _userRepo = null!;
    private Mock<IOtpRepository> _otpRepo = null!;
    private Mock<IEmailService> _emailService = null!;
    private Mock<IEmailTemplateService> _templateService = null!;
    private Mock<ILogger<ForgotPasswordCommandHandler>> _logger = null!;
    private ForgotPasswordCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = new Mock<IUserRepository>();
        _otpRepo = new Mock<IOtpRepository>();
        _emailService = new Mock<IEmailService>();
        _templateService = new Mock<IEmailTemplateService>();
        _templateService.Setup(t => t.Render(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>())).Returns("<html>OTP</html>");
        _logger = new Mock<ILogger<ForgotPasswordCommandHandler>>();
        _handler = new ForgotPasswordCommandHandler(_userRepo.Object, _otpRepo.Object, _emailService.Object, _templateService.Object, _logger.Object);
    }

    [Test]
    public async Task Handle_ExistingUser_GeneratesOtp()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "john@test.com", FullName = "John Doe", IsActive = true };
        _userRepo.Setup(r => r.GetByEmailAsync("john@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var cmd = new ForgotPasswordCommand("john@test.com");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Message.Should().Contain("password reset");
        _userRepo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_NonExistentEmail_ReturnsGracefully()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var cmd = new ForgotPasswordCommand("notfound@test.com");

        // Should NOT throw — graceful handling (no user enumeration)
        var result = await _handler.Handle(cmd, CancellationToken.None);
        result.Message.Should().NotBeNull();
    }
}

#endregion

#region LockUser Tests

[TestFixture]
public class LockUserHandlerTests
{
    private Mock<IUserRepository> _userRepo = null!;
    private Mock<IRabbitMqPublisher> _publisher = null!;
    private Mock<ILogger<LockUserCommandHandler>> _logger = null!;
    private LockUserCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = new Mock<IUserRepository>();
        _publisher = new Mock<IRabbitMqPublisher>();
        _logger = new Mock<ILogger<LockUserCommandHandler>>();
        _handler = new LockUserCommandHandler(_userRepo.Object, _publisher.Object, _logger.Object);
    }

    [Test]
    public async Task Handle_LockUser_SetsLockout()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "john@test.com", FullName = "John", IsActive = true };
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var cmd = new LockUserCommand(user.Id, true, "Suspicious activity", Guid.NewGuid());
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Message.Should().Contain("locked");
        _userRepo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_UnlockUser_ClearsLockout()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "john@test.com",
            FullName = "John",
            IsActive = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddDays(1)
        };
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var cmd = new LockUserCommand(user.Id, false, null, Guid.NewGuid());
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Message.Should().Contain("unlock");
    }

    [Test]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var cmd = new LockUserCommand(Guid.NewGuid(), true, "test", Guid.NewGuid());
        var act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

#endregion

#region GetAllUsers Query Tests

[TestFixture]
public class GetAllUsersQueryTests
{
    private Mock<IUserRepository> _userRepo = null!;
    private Mock<IMapper> _mapper = null!;
    private GetAllUsersQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = new Mock<IUserRepository>();
        _mapper = new Mock<IMapper>();
        _handler = new GetAllUsersQueryHandler(_userRepo.Object, _mapper.Object);
    }

    [Test]
    public async Task Handle_ReturnsPagedResults()
    {
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), FullName = "User 1", Email = "u1@test.com", Role = UserRole.Customer },
            new() { Id = Guid.NewGuid(), FullName = "User 2", Email = "u2@test.com", Role = UserRole.Dealer }
        };
        _userRepo.Setup(r => r.GetAllAsync(1, 10, null, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((users.AsReadOnly(), 2));
        _mapper.Setup(m => m.Map<IReadOnlyList<UserDto>>(users)).Returns(new List<UserDto>
        {
            new() { Id = users[0].Id, FullName = "User 1" },
            new() { Id = users[1].Id, FullName = "User 2" }
        });

        var query = new GetAllUsersQuery(1, 10);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }
}

#endregion
