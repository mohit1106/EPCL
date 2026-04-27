using AIAnalyticsService.Domain.Entities;
using AIAnalyticsService.Domain.Interfaces;
using MediatR;

namespace AIAnalyticsService.Application.Queries;

public record GetChatHistoryQuery(Guid UserId, string? SessionId) : IRequest<List<ConversationMessage>>;

public class GetChatHistoryQueryHandler : IRequestHandler<GetChatHistoryQuery, List<ConversationMessage>>
{
    private readonly IConversationRepository _repo;

    public GetChatHistoryQueryHandler(IConversationRepository repo) => _repo = repo;

    public Task<List<ConversationMessage>> Handle(GetChatHistoryQuery request, CancellationToken ct)
        => _repo.GetUserHistoryAsync(request.UserId, request.SessionId, ct);
}
