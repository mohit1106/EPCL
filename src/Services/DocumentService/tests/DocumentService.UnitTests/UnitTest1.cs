using FluentAssertions;
using Moq;
using DocumentService.Application.Commands;
using DocumentService.Application.DTOs;
using DocumentService.Domain.Entities;
using DocumentService.Domain.Interfaces;
using DocumentService.Domain.Repositories;

namespace DocumentService.UnitTests;

[TestFixture]
public class DocumentCommandTests
{
    private Mock<IDocumentRepository> _repo = null!;
    private Mock<IDocumentAccessLogRepository> _logRepo = null!;
    private Mock<IFileStorageService> _storage = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IDocumentRepository>();
        _logRepo = new Mock<IDocumentAccessLogRepository>();
        _storage = new Mock<IFileStorageService>();
    }

    [Test]
    public async Task UploadDocument_SavesFileAndReturnsDto()
    {
        var handler = new UploadDocumentCommandHandler(_repo.Object, _storage.Object);

        using var ms = new MemoryStream();
        _storage.Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/path/to/file");
        _repo.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var cmd = new UploadDocumentCommand(ms, "test.pdf", 1024, "application/pdf", "User", Guid.NewGuid(), "GovId", null, null, Guid.NewGuid());
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().Be("test.pdf");
        _storage.Verify(s => s.SaveFileAsync(It.IsAny<Stream>(), It.Is<string>(f => f.EndsWith(".pdf")), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task VerifyDocument_Found_UpdatesProperties()
    {
        var handler = new DocumentCommandHandlers(_repo.Object, _logRepo.Object);
        var id = Guid.NewGuid();
        var doc = new Document { Id = id, IsVerified = false };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(doc);

        var userId = Guid.NewGuid();
        await handler.Handle(new VerifyDocumentCommand(id, userId, "Looks good"), CancellationToken.None);

        doc.IsVerified.Should().BeTrue();
        doc.VerifiedByUserId.Should().Be(userId);
        doc.Notes.Should().Be("Looks good");
        _repo.Verify(r => r.UpdateAsync(doc, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SoftDeleteDocument_Found_SetsIsActiveFalse()
    {
        var handler = new DocumentCommandHandlers(_repo.Object, _logRepo.Object);
        var id = Guid.NewGuid();
        var doc = new Document { Id = id, IsActive = true };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(doc);

        await handler.Handle(new SoftDeleteDocumentCommand(id), CancellationToken.None);

        doc.IsActive.Should().BeFalse();
        _repo.Verify(r => r.UpdateAsync(doc, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LogDocumentAccess_AddsLog()
    {
        var handler = new DocumentCommandHandlers(_repo.Object, _logRepo.Object);
        var cmd = new LogDocumentAccessCommand(Guid.NewGuid(), Guid.NewGuid(), "Read", "127.0.0.1");

        await handler.Handle(cmd, CancellationToken.None);

        _logRepo.Verify(r => r.AddAsync(It.Is<DocumentAccessLog>(l => 
            l.DocumentId == cmd.DocumentId && 
            l.AccessType == "Read" && 
            l.IpAddress == "127.0.0.1"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
