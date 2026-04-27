using MediatR;
using ReportingService.Domain.Entities;
using ReportingService.Domain.Interfaces;

namespace ReportingService.Application.Queries;

public record GetAtRiskStockPredictionsQuery(int DaysThreshold, Guid? StationId) : IRequest<IReadOnlyList<StockPrediction>>;

public class GetAtRiskStockPredictionsQueryHandler(IStockPredictionRepository repo)
    : IRequestHandler<GetAtRiskStockPredictionsQuery, IReadOnlyList<StockPrediction>>
{
    public Task<IReadOnlyList<StockPrediction>> Handle(GetAtRiskStockPredictionsQuery request, CancellationToken ct)
    {
        return repo.GetAtRiskAsync(request.DaysThreshold, request.StationId, ct);
    }
}
