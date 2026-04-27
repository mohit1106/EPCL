using AIAnalyticsService.Domain.Interfaces;
using MediatR;

namespace AIAnalyticsService.Application.Commands;

public record ClearChatHistoryCommand(Guid UserId, string? SessionId) : IRequest;

public class ClearChatHistoryCommandHandler : IRequestHandler<ClearChatHistoryCommand>
{
    private readonly IConversationRepository _repo;

    public ClearChatHistoryCommandHandler(IConversationRepository repo) => _repo = repo;

    public Task Handle(ClearChatHistoryCommand request, CancellationToken ct)
        => _repo.ClearHistoryAsync(request.UserId, request.SessionId, ct);
}
