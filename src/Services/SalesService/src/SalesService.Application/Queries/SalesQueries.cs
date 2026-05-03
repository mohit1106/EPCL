using MediatR;
using SalesService.Application.Common;
using SalesService.Application.DTOs;
using SalesService.Domain.Enums;
using SalesService.Domain.Exceptions;
using SalesService.Domain.Interfaces;

namespace SalesService.Application.Queries;

// ── Transactions ───────────────────────────────────────────────────
public record GetTransactionsQuery(int Page = 1, int PageSize = 20, Guid? StationId = null,
    Guid? DealerId = null, Guid? CustomerId = null, string? VehicleNumber = null,
    string? Status = null, DateTimeOffset? DateFrom = null, DateTimeOffset? DateTo = null,
    Guid? FuelTypeId = null) : IRequest<PagedResult<TransactionDto>>;

public class GetTransactionsHandler(ITransactionRepository txRepo) : IRequestHandler<GetTransactionsQuery, PagedResult<TransactionDto>>
{
    public async Task<PagedResult<TransactionDto>> Handle(GetTransactionsQuery q, CancellationToken ct)
    {
        TransactionStatus? statusFilter = null;
        if (q.Status != null && Enum.TryParse<TransactionStatus>(q.Status, out var s)) statusFilter = s;

        var (items, total) = await txRepo.GetPagedAsync(q.Page, q.PageSize, q.StationId, q.DealerId,
            q.CustomerId, q.VehicleNumber, statusFilter, q.DateFrom, q.DateTo, q.FuelTypeId, ct);

        return new PagedResult<TransactionDto>
        {
            Items = items.Select(t => MapTx(t)).ToList(),
            TotalCount = total, Page = q.Page, PageSize = q.PageSize
        };
    }

    private static TransactionDto MapTx(Domain.Entities.Transaction t) => new(
        t.Id, t.ReceiptNumber, t.StationId, t.PumpId, t.FuelTypeId,
        t.DealerUserId, t.CustomerUserId, t.VehicleNumber,
        t.QuantityLitres, t.PricePerLitre, t.TotalAmount,
        t.PaymentMethod.ToString(), t.PaymentReferenceId, t.Status.ToString(),
        t.FraudCheckStatus.ToString(), t.LoyaltyPointsEarned, t.LoyaltyPointsRedeemed,
        t.Timestamp, t.IsVoided);
}

public record GetTransactionByIdQuery(Guid TransactionId) : IRequest<TransactionDto>;

public class GetTransactionByIdHandler(ITransactionRepository txRepo) : IRequestHandler<GetTransactionByIdQuery, TransactionDto>
{
    public async Task<TransactionDto> Handle(GetTransactionByIdQuery q, CancellationToken ct)
    {
        var t = await txRepo.GetByIdAsync(q.TransactionId, ct) ?? throw new NotFoundException("Transaction", q.TransactionId);
        return new TransactionDto(t.Id, t.ReceiptNumber, t.StationId, t.PumpId, t.FuelTypeId,
            t.DealerUserId, t.CustomerUserId, t.VehicleNumber, t.QuantityLitres, t.PricePerLitre, t.TotalAmount,
            t.PaymentMethod.ToString(), t.PaymentReferenceId, t.Status.ToString(), t.FraudCheckStatus.ToString(),
            t.LoyaltyPointsEarned, t.LoyaltyPointsRedeemed, t.Timestamp, t.IsVoided);
    }
}

// ── Pumps ──────────────────────────────────────────────────────────
public record GetPumpsByStationQuery(Guid StationId) : IRequest<IReadOnlyList<PumpDto>>;

public class GetPumpsByStationHandler(IPumpRepository pumpRepo) : IRequestHandler<GetPumpsByStationQuery, IReadOnlyList<PumpDto>>
{
    public async Task<IReadOnlyList<PumpDto>> Handle(GetPumpsByStationQuery q, CancellationToken ct)
    {
        var pumps = await pumpRepo.GetByStationIdAsync(q.StationId, ct);
        return pumps.Select(p => new PumpDto(p.Id, p.StationId, p.FuelTypeId, p.PumpName,
            p.NozzleCount, p.Status.ToString(), p.LastServiced, p.NextServiceDue, p.CreatedAt)).ToList();
    }
}

// ── Fuel Prices ────────────────────────────────────────────────────
public record GetActiveFuelPricesQuery() : IRequest<IReadOnlyList<FuelPriceDto>>;

public class GetActiveFuelPricesHandler(IFuelPriceRepository priceRepo) : IRequestHandler<GetActiveFuelPricesQuery, IReadOnlyList<FuelPriceDto>>
{
    public async Task<IReadOnlyList<FuelPriceDto>> Handle(GetActiveFuelPricesQuery q, CancellationToken ct)
    {
        var prices = await priceRepo.GetAllActiveAsync(ct);
        return prices.Select(p => new FuelPriceDto(p.Id, p.FuelTypeId, p.PricePerLitre,
            p.EffectiveFrom, p.IsActive, p.SetByUserId, p.CreatedAt)).ToList();
    }
}

// ── Shifts ─────────────────────────────────────────────────────────
public record GetActiveShiftQuery(Guid DealerUserId) : IRequest<ShiftDto?>;

public class GetActiveShiftHandler(IShiftRepository shiftRepo) : IRequestHandler<GetActiveShiftQuery, ShiftDto?>
{
    public async Task<ShiftDto?> Handle(GetActiveShiftQuery q, CancellationToken ct)
    {
        var s = await shiftRepo.GetActiveShiftAsync(q.DealerUserId, ct);
        if (s == null) return null;
        return new ShiftDto(s.Id, s.DealerUserId, s.StationId, s.StartedAt, s.EndedAt,
            s.OpeningStockJson, s.ClosingStockJson, s.TotalLitresSold, s.TotalRevenue, s.TotalTransactions, s.DiscrepancyFlagged);
    }
}

public record GetShiftHistoryQuery(Guid StationId, int Page = 1, int PageSize = 50) : IRequest<IReadOnlyList<ShiftDto>>;

public class GetShiftHistoryHandler(IShiftRepository shiftRepo) : IRequestHandler<GetShiftHistoryQuery, IReadOnlyList<ShiftDto>>
{
    public async Task<IReadOnlyList<ShiftDto>> Handle(GetShiftHistoryQuery q, CancellationToken ct)
    {
        var shifts = await shiftRepo.GetByStationAsync(q.StationId, q.Page, q.PageSize, ct);
        return shifts.Select(s => new ShiftDto(s.Id, s.DealerUserId, s.StationId, s.StartedAt, s.EndedAt,
            s.OpeningStockJson, s.ClosingStockJson, s.TotalLitresSold, s.TotalRevenue, s.TotalTransactions, s.DiscrepancyFlagged)).ToList();
    }
}

public record GetAllShiftsQuery(int Page = 1, int PageSize = 50, Guid? StationId = null) : IRequest<PagedResult<ShiftDto>>;

public class GetAllShiftsHandler(IShiftRepository shiftRepo) : IRequestHandler<GetAllShiftsQuery, PagedResult<ShiftDto>>
{
    public async Task<PagedResult<ShiftDto>> Handle(GetAllShiftsQuery q, CancellationToken ct)
    {
        var (items, total) = await shiftRepo.GetAllPagedAsync(q.Page, q.PageSize, q.StationId, ct);
        return new PagedResult<ShiftDto>
        {
            Items = items.Select(s => new ShiftDto(s.Id, s.DealerUserId, s.StationId, s.StartedAt, s.EndedAt,
                s.OpeningStockJson, s.ClosingStockJson, s.TotalLitresSold, s.TotalRevenue, s.TotalTransactions, s.DiscrepancyFlagged)).ToList(),
            TotalCount = total, Page = q.Page, PageSize = q.PageSize
        };
    }
}

// ── Vehicles ───────────────────────────────────────────────────────
public record GetCustomerVehiclesQuery(Guid CustomerId) : IRequest<IReadOnlyList<VehicleDto>>;

public class GetCustomerVehiclesHandler(IRegisteredVehicleRepository vRepo) : IRequestHandler<GetCustomerVehiclesQuery, IReadOnlyList<VehicleDto>>
{
    public async Task<IReadOnlyList<VehicleDto>> Handle(GetCustomerVehiclesQuery q, CancellationToken ct)
    {
        var vs = await vRepo.GetByCustomerAsync(q.CustomerId, ct);
        return vs.Select(v => new VehicleDto(v.Id, v.CustomerId, v.RegistrationNumber,
            v.FuelTypePreference, v.VehicleType.ToString(), v.Nickname, v.IsActive, v.RegisteredAt)).ToList();
    }
}

// ── Fleet ──────────────────────────────────────────────────────────
public record GetFleetAccountsQuery() : IRequest<IReadOnlyList<FleetAccountDto>>;

public class GetFleetAccountsHandler(IFleetAccountRepository faRepo) : IRequestHandler<GetFleetAccountsQuery, IReadOnlyList<FleetAccountDto>>
{
    public async Task<IReadOnlyList<FleetAccountDto>> Handle(GetFleetAccountsQuery q, CancellationToken ct)
    {
        var fas = await faRepo.GetAllAsync(ct);
        return fas.Select(f => new FleetAccountDto(f.Id, f.CompanyName, f.ContactUserId,
            f.CreditLimit, f.CurrentBalance, f.IsActive, f.CreatedAt)).ToList();
    }
}

// ── Wallet ─────────────────────────────────────────────────────────
public record GetWalletBalanceQuery(Guid CustomerId) : IRequest<WalletDto>;

public class GetWalletBalanceHandler(ICustomerWalletRepository walletRepo) : IRequestHandler<GetWalletBalanceQuery, WalletDto>
{
    public async Task<WalletDto> Handle(GetWalletBalanceQuery q, CancellationToken ct)
    {
        var w = await walletRepo.GetByCustomerIdAsync(q.CustomerId, ct)
            ?? throw new NotFoundException("Wallet", q.CustomerId);
        return new WalletDto(w.Id, w.CustomerId, w.Balance, w.TotalLoaded, w.IsActive);
    }
}

public record GetWalletHistoryQuery(Guid CustomerId, int Page = 1, int PageSize = 20) : IRequest<IReadOnlyList<WalletTransactionDto>>;

public class GetWalletHistoryHandler(ICustomerWalletRepository walletRepo, IWalletTransactionRepository wtRepo)
    : IRequestHandler<GetWalletHistoryQuery, IReadOnlyList<WalletTransactionDto>>
{
    public async Task<IReadOnlyList<WalletTransactionDto>> Handle(GetWalletHistoryQuery q, CancellationToken ct)
    {
        var w = await walletRepo.GetByCustomerIdAsync(q.CustomerId, ct)
            ?? throw new NotFoundException("Wallet", q.CustomerId);
        var items = await wtRepo.GetByWalletIdAsync(w.Id, q.Page, q.PageSize, ct);
        return items.Select(t => new WalletTransactionDto(t.Id, t.WalletId, t.Type.ToString(), t.Amount,
            t.BalanceAfter, t.RazorpayOrderId, t.RazorpayPaymentId, t.Status.ToString(),
            t.SaleTransactionId, t.Description, t.CreatedAt)).ToList();
    }
}

// ── Pending Wallet Payment Requests ────────────────────────────────
public record GetPendingPaymentRequestsQuery(Guid CustomerId) : IRequest<IReadOnlyList<WalletPaymentRequestDto>>;

public class GetPendingPaymentRequestsHandler(IWalletPaymentRequestRepository reqRepo)
    : IRequestHandler<GetPendingPaymentRequestsQuery, IReadOnlyList<WalletPaymentRequestDto>>
{
    public async Task<IReadOnlyList<WalletPaymentRequestDto>> Handle(GetPendingPaymentRequestsQuery q, CancellationToken ct)
    {
        var requests = await reqRepo.GetPendingByCustomerAsync(q.CustomerId, ct);
        return requests.Select(r => new WalletPaymentRequestDto(r.Id, r.SaleTransactionId, r.CustomerId,
            r.DealerUserId, r.StationId, r.Amount, r.Status, r.Description,
            r.VehicleNumber, r.FuelTypeName, r.QuantityLitres, r.PaymentMethod,
            r.CreatedAt, r.ExpiresAt)).ToList();
    }
}

// ── All Payment Requests (including history) ───────────────────────
public record GetAllPaymentRequestsQuery(Guid CustomerId) : IRequest<IReadOnlyList<WalletPaymentRequestDto>>;

public class GetAllPaymentRequestsHandler(IWalletPaymentRequestRepository reqRepo)
    : IRequestHandler<GetAllPaymentRequestsQuery, IReadOnlyList<WalletPaymentRequestDto>>
{
    public async Task<IReadOnlyList<WalletPaymentRequestDto>> Handle(GetAllPaymentRequestsQuery q, CancellationToken ct)
    {
        var requests = await reqRepo.GetAllByCustomerAsync(q.CustomerId, ct);
        return requests.Select(r => new WalletPaymentRequestDto(r.Id, r.SaleTransactionId, r.CustomerId,
            r.DealerUserId, r.StationId, r.Amount, r.Status, r.Description,
            r.VehicleNumber, r.FuelTypeName, r.QuantityLitres, r.PaymentMethod,
            r.CreatedAt, r.ExpiresAt)).ToList();
    }
}

// ── Get Payment Request By Id ──────────────────────────────────────
public record GetPaymentRequestByIdQuery(Guid CustomerId, Guid RequestId) : IRequest<WalletPaymentRequestDto?>;

public class GetPaymentRequestByIdHandler(IWalletPaymentRequestRepository reqRepo)
    : IRequestHandler<GetPaymentRequestByIdQuery, WalletPaymentRequestDto?>
{
    public async Task<WalletPaymentRequestDto?> Handle(GetPaymentRequestByIdQuery q, CancellationToken ct)
    {
        var r = await reqRepo.GetByIdAsync(q.RequestId, ct);
        if (r == null || r.CustomerId != q.CustomerId) return null;
        return new WalletPaymentRequestDto(r.Id, r.SaleTransactionId, r.CustomerId,
            r.DealerUserId, r.StationId, r.Amount, r.Status, r.Description,
            r.VehicleNumber, r.FuelTypeName, r.QuantityLitres, r.PaymentMethod,
            r.CreatedAt, r.ExpiresAt);
    }
}

// ── Daily Summary ──────────────────────────────────────────────────
public record GetDailySummaryQuery(Guid StationId, string Date) : IRequest<DailySummaryDto>;

public class GetDailySummaryHandler(ITransactionRepository txRepo)
    : IRequestHandler<GetDailySummaryQuery, DailySummaryDto>
{
    public async Task<DailySummaryDto> Handle(GetDailySummaryQuery q, CancellationToken ct)
    {
        var date = DateTimeOffset.Parse(q.Date);
        var dayStart = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var (items, _) = await txRepo.GetPagedAsync(1, 10000, q.StationId,
            dateFrom: dayStart, dateTo: dayEnd, ct: ct);

        var totalTx = items.Count;
        var totalLitres = items.Sum(t => t.QuantityLitres);
        var totalRevenue = items.Sum(t => t.TotalAmount);

        var hourlyData = Enumerable.Range(0, 24).Select(h =>
        {
            var hourTx = items.Where(t => t.Timestamp.Hour == h).ToList();
            return new HourlyDataDto(h, hourTx.Count, hourTx.Sum(t => t.QuantityLitres), hourTx.Sum(t => t.TotalAmount));
        }).ToList();

        return new DailySummaryDto(q.Date, totalTx, totalLitres, totalRevenue, hourlyData);
    }
}

// ── Get Wallet Balance for Dealer (to show during sale) ────────────
public record GetCustomerWalletBalanceQuery(Guid CustomerId) : IRequest<WalletDto?>;

public class GetCustomerWalletBalanceHandler(ICustomerWalletRepository walletRepo)
    : IRequestHandler<GetCustomerWalletBalanceQuery, WalletDto?>
{
    public async Task<WalletDto?> Handle(GetCustomerWalletBalanceQuery q, CancellationToken ct)
    {
        var w = await walletRepo.GetByCustomerIdAsync(q.CustomerId, ct);
        return w == null ? null : new WalletDto(w.Id, w.CustomerId, w.Balance, w.TotalLoaded, w.IsActive);
    }
}

