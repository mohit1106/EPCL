using MediatR;
using ReportingService.Domain.Entities;
using ReportingService.Domain.Interfaces;

namespace ReportingService.Application.Queries;

public record GetStockPredictionsQuery(Guid? StationId) : IRequest<IReadOnlyList<StockPrediction>>;

public class GetStockPredictionsQueryHandler(IStockPredictionRepository repo)
    : IRequestHandler<GetStockPredictionsQuery, IReadOnlyList<StockPrediction>>
{
    public Task<IReadOnlyList<StockPrediction>> Handle(GetStockPredictionsQuery request, CancellationToken ct)
    {
        return repo.GetAllAsync(request.StationId, ct);
    }
}
