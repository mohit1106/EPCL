using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using DocumentService.Application.Commands;
using DocumentService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DocumentService.API.Controllers
{
    [ApiController]
    [Route("api/documents")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DocumentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(26_214_400)] // 25MB
        public async Task<IActionResult> Upload(
            [FromForm] IFormFile file,
            [FromForm] string entityType,
            [FromForm] Guid entityId,
            [FromForm] string documentType,
            [FromForm] DateTime? expiryDate,
            [FromForm] string? notes)
        {
            if (file == null || file.Length == 0) return BadRequest("File is required.");
            if (string.IsNullOrEmpty(entityType)) return BadRequest("EntityType is required.");
            if (entityId == Guid.Empty) return BadRequest("EntityId is required.");
            if (string.IsNullOrEmpty(documentType)) return BadRequest("DocumentType is required.");

            // Validations
            var validTypes = new[] { "PetroleumLicense", "TankCalibrationCert", "TankerInvoice", "InsuranceCertificate", "PPACSubmission", "WeightsAndMeasuresCert", "FireSafetyCert", "PollutionControlCert", "AuditReport" };
            if (!validTypes.Contains(documentType)) return BadRequest("Invalid DocumentType.");

            // Authorization check
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var userIdStr = User.FindFirst("userId")?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            if (userRole == "Dealer")
            {
                var stationIdStr = User.FindFirst("stationId")?.Value;
                if (!Guid.TryParse(stationIdStr, out var stationId) || stationId != entityId)
                {
                    return Forbid();
                }
            }
            else if (userRole != "Admin" && userRole != "SuperAdmin")
            {
                return Forbid();
            } // Admin/SuperAdmin can upload for any entity

            using var stream = file.OpenReadStream();
            var command = new UploadDocumentCommand(
                stream, file.FileName, file.Length, file.ContentType, 
                entityType, entityId, documentType, expiryDate, notes, userId);

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? entityType, [FromQuery] Guid? entityId, [FromQuery] string? documentType)
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "Dealer")
            {
                var stationIdStr = User.FindFirst("stationId")?.Value;
                if (!Guid.TryParse(stationIdStr, out var stationId)) return Unauthorized();
                
                // Deal can only view their own station documents
                entityType = "Station";
                entityId = stationId;
            }

            var query = new GetDocumentsQuery(entityType, entityId, documentType);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("expiring")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetExpiring([FromQuery] int daysAhead = 30)
        {
            var query = new GetExpiringDocumentsQuery(daysAhead);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(Guid id)
        {
            var userIdStr = User.FindFirst("userId")?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _mediator.Send(new GetDocumentFileQuery(id, userId, ipAddress, false));
            if (result == null) return NotFound();

            return File(result.Value.Stream, result.Value.MimeType, result.Value.FileName);
        }

        [HttpGet("{id}/preview")]
        public async Task<IActionResult> Preview(Guid id)
        {
            var userIdStr = User.FindFirst("userId")?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _mediator.Send(new GetDocumentFileQuery(id, userId, ipAddress, true));
            if (result == null) return NotFound();

            // Set Content-Disposition inline for browser preview
            Response.Headers.Add("Content-Disposition", $"inline; filename=\"{result.Value.FileName}\"");
            return File(result.Value.Stream, result.Value.MimeType);
        }

        [HttpPut("{id}/verify")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Verify(Guid id, [FromBody] VerifyRequest request)
        {
            var userIdStr = User.FindFirst("userId")?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var command = new VerifyDocumentCommand(id, userId, request.Notes);
            var result = await _mediator.Send(command);

            return result ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new SoftDeleteDocumentCommand(id);
            var result = await _mediator.Send(command);

            return result ? NoContent() : NotFound();
        }

        public class VerifyRequest
        {
            public string? Notes { get; set; }
        }
    }
}
