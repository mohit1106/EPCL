using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesService.Application.DTOs;
using SalesService.Application.Interfaces;
using SalesService.Domain.Entities;
using SalesService.Domain.Enums;
using SalesService.Domain.Events;
using SalesService.Domain.Interfaces;
using System.Security.Claims;

namespace SalesService.API.Controllers;

[ApiController]
[Route("api/sales/parking")]
[Authorize]
public class ParkingController(
    IParkingSlotRepository slotRepo,
    IParkingBookingRepository bookingRepo,
    IShiftRepository shiftRepo,
    IRazorpayService razorpay,
    IRabbitMqPublisher publisher) : ControllerBase
{
    // ── Pricing table ──────────────────────────────────────────────
    private static readonly Dictionary<string, Dictionary<int, decimal>> PricingTable = new()
    {
        ["TwoWheeler"] = new() { [1] = 10m, [2] = 20m, [4] = 35m, [24] = 80m },
        ["FourWheeler"] = new() { [1] = 20m, [2] = 40m, [4] = 80m, [24] = 200m },
        ["HGV"] = new() { [1] = 50m, [2] = 100m, [4] = 180m, [24] = 400m },
    };

    /// <summary>Get available parking slots for a station.</summary>
    [HttpGet("stations/{stationId:guid}/slots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSlots(Guid stationId)
    {
        var slots = await slotRepo.GetByStationAsync(stationId);
        var dtos = slots.Select(s => new ParkingSlotDto(
            s.Id, s.StationId, s.SlotType.ToString(), s.SlotNumber, s.IsAvailable)).ToList();
        return Ok(dtos);
    }

    /// <summary>Get parking pricing table.</summary>
    [HttpGet("pricing")]
    [AllowAnonymous]
    public IActionResult GetPricing() => Ok(PricingTable);

    /// <summary>Create a parking booking and Razorpay order.</summary>
    [HttpPost("book")]
    public async Task<IActionResult> BookParking([FromBody] CreateParkingBookingRequest req)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Validate slot type and duration
        if (!PricingTable.ContainsKey(req.SlotType))
            return BadRequest(new { message = $"Invalid slot type: {req.SlotType}" });
        if (!PricingTable[req.SlotType].ContainsKey(req.DurationHours))
            return BadRequest(new { message = $"Invalid duration: {req.DurationHours}h. Valid: 1, 2, 4, 24" });

        var amount = PricingTable[req.SlotType][req.DurationHours];

        // Find an available slot
        var slots = await slotRepo.GetByStationAsync(req.StationId);
        var slotTypeEnum = Enum.Parse<ParkingSlotType>(req.SlotType);
        var availableSlot = slots.FirstOrDefault(s => s.SlotType == slotTypeEnum && s.IsAvailable);
        if (availableSlot == null)
            return BadRequest(new { message = "No available parking slots for this type at this station" });

        // Create Razorpay order
        var order = await razorpay.CreateOrderAsync(amount, "INR");

        // Create booking
        var booking = new ParkingBooking
        {
            Id = Guid.NewGuid(),
            ParkingSlotId = availableSlot.Id,
            StationId = req.StationId,
            CustomerId = userId,
            SlotType = slotTypeEnum,
            DurationHours = req.DurationHours,
            Amount = amount,
            Status = ParkingBookingStatus.Initiated,
            RazorpayOrderId = order.OrderId,
            BookedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(req.DurationHours),
        };
        await bookingRepo.AddAsync(booking);

        // Reserve the slot
        availableSlot.IsAvailable = false;
        await slotRepo.UpdateAsync(availableSlot);

        return Ok(new
        {
            bookingId = booking.Id,
            orderId = order.OrderId,
            amount = order.Amount,
            currency = order.Currency,
            keyId = order.KeyId,
            slotNumber = availableSlot.SlotNumber,
        });
    }

    /// <summary>Confirm parking payment after Razorpay callback.</summary>
    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmParkingPaymentRequest req)
    {
        var booking = await bookingRepo.GetByRazorpayOrderIdAsync(req.OrderId);
        if (booking == null) return NotFound(new { message = "Booking not found" });

        // Verify signature
        var valid = razorpay.VerifyPaymentSignature(req.OrderId, req.PaymentId, req.Signature);
        if (!valid)
        {
            booking.Status = ParkingBookingStatus.Cancelled;
            await bookingRepo.UpdateAsync(booking);
            // Release the slot
            var slot = await slotRepo.GetByIdAsync(booking.ParkingSlotId);
            if (slot != null) { slot.IsAvailable = true; await slotRepo.UpdateAsync(slot); }
            return BadRequest(new { message = "Payment verification failed" });
        }

        booking.Status = ParkingBookingStatus.Confirmed;
        booking.RazorpayPaymentId = req.PaymentId;
        await bookingRepo.UpdateAsync(booking);

        // Publish notification for email confirmation
        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Customer";
            await publisher.PublishAsync(new ParkingBookingConfirmedEvent
            {
                EventType = "ParkingBookingConfirmed",
                BookingId = booking.Id,
                StationId = booking.StationId,
                SlotType = booking.SlotType.ToString(),
                SlotNumber = booking.ParkingSlot?.SlotNumber ?? "N/A",
                DurationHours = booking.DurationHours,
                Amount = booking.Amount,
                BookedAt = booking.BookedAt,
                ExpiresAt = booking.ExpiresAt,
                UserEmail = userEmail,
                UserName = userName,
            }, "parking.booking.confirmed");
        }
        catch { /* Non-critical: email notification failure should not block booking */ }

        return Ok(new
        {
            message = "Parking booked successfully!",
            bookingId = booking.Id,
            status = booking.Status.ToString(),
            slotType = booking.SlotType.ToString(),
            slotNumber = booking.ParkingSlot?.SlotNumber ?? "N/A",
            durationHours = booking.DurationHours,
            amount = booking.Amount,
            bookedAt = booking.BookedAt,
            expiresAt = booking.ExpiresAt,
            paymentId = req.PaymentId,
        });
    }

    /// <summary>Get current customer's parking bookings.</summary>
    [HttpGet("my-bookings")]
    public async Task<IActionResult> GetMyBookings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var bookings = await bookingRepo.GetByCustomerAsync(userId, page, pageSize);
        var dtos = bookings.Select(b => new ParkingBookingDto(
            b.Id, b.ParkingSlotId, b.StationId, b.CustomerId,
            b.SlotType.ToString(), b.DurationHours, b.Amount, b.Status.ToString(),
            b.RazorpayOrderId, b.RazorpayPaymentId, b.BookedAt, b.ExpiresAt)).ToList();
        return Ok(dtos);
    }

    /// <summary>Get parking bookings for dealer's active station.</summary>
    [HttpGet("station-bookings")]
    [Authorize(Roles = "Dealer")]
    public async Task<IActionResult> GetStationBookings([FromQuery] Guid? stationId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var dealerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var resolvedStationId = stationId;

        if (!resolvedStationId.HasValue || resolvedStationId == Guid.Empty)
        {
            var shift = await shiftRepo.GetActiveShiftAsync(dealerId);
            if (shift != null)
                resolvedStationId = shift.StationId;
        }

        if (!resolvedStationId.HasValue || resolvedStationId == Guid.Empty)
            return Ok(new List<ParkingBookingDto>()); // Return empty if no station found

        var bookings = await bookingRepo.GetByStationAsync(resolvedStationId.Value, page, pageSize);
        var dtos = bookings.Select(b => new ParkingBookingDto(
            b.Id, b.ParkingSlotId, b.StationId, b.CustomerId,
            b.SlotType.ToString(), b.DurationHours, b.Amount, b.Status.ToString(),
            b.RazorpayOrderId, b.RazorpayPaymentId, b.BookedAt, b.ExpiresAt)).ToList();
        return Ok(dtos);
    }

    /// <summary>Toggle parking availability for the dealer's station.</summary>
    [HttpPost("toggle-availability")]
    [Authorize(Roles = "Dealer")]
    public async Task<IActionResult> ToggleAvailability([FromBody] ToggleParkingRequest req)
    {
        var dealerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        Guid? resolvedStationId = req.StationId;

        if (!resolvedStationId.HasValue || resolvedStationId == Guid.Empty)
        {
            var shift = await shiftRepo.GetActiveShiftAsync(dealerId);
            if (shift != null)
                resolvedStationId = shift.StationId;
        }

        if (!resolvedStationId.HasValue || resolvedStationId == Guid.Empty)
            return BadRequest(new { message = "Could not determine station. Please start a shift or ensure a station is assigned." });

        var slots = await slotRepo.GetByStationAsync(resolvedStationId.Value);
        foreach (var slot in slots)
        {
            slot.IsAvailable = req.IsAvailable;
            await slotRepo.UpdateAsync(slot);
        }

        return Ok(new { message = $"Parking availability set to {(req.IsAvailable ? "Available" : "Disabled")} for all slots." });
    }
}

public record ToggleParkingRequest(bool IsAvailable, Guid? StationId = null);
