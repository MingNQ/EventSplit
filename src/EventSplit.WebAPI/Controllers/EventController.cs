using System.Security.Claims;
using EventSplit.Application.DTOs;
using EventSplit.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using EventSplit.WebAPI.Hubs;

namespace EventSplit.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventController : ControllerBase
{
    private readonly EventService _eventService;
    private readonly IHubContext<PaymentHub> _hubContext;

    public EventController(EventService eventService, IHubContext<PaymentHub> hubContext)
    {
        _eventService = eventService;
        _hubContext = hubContext;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        try
        {
            var result = await _eventService.CreateEventAsync(request, GetUserId());
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet]
    public async Task<IActionResult> GetMyEvents()
    {
        var result = await _eventService.GetMyEventsAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        try
        {
            var result = await _eventService.GetEventDetailAsync(id, GetUserId());
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        try
        {
            var result = await _eventService.UpdateEventAsync(id, request, GetUserId());
            await _hubContext.Clients.Group(id.ToString()).SendAsync("EventUpdated", result);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _eventService.DeleteEventAsync(id, GetUserId());
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
    }

    [HttpPost("{id}/participants")]
    public async Task<IActionResult> AddParticipant(Guid id, [FromBody] AddParticipantRequest request)
    {
        try
        {
            var result = await _eventService.AddParticipantAsync(id, request, GetUserId());
            await _hubContext.Clients.Group(id.ToString()).SendAsync("ParticipantAdded", result);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
    }

    [HttpDelete("{id}/participants/{participantId}")]
    public async Task<IActionResult> RemoveParticipant(Guid id, Guid participantId)
    {
        try
        {
            await _eventService.RemoveParticipantAsync(id, participantId, GetUserId());
            await _hubContext.Clients.Group(id.ToString()).SendAsync("ParticipantRemoved", participantId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
    }

    [HttpPost("{id}/participants/{participantId}/confirm")]
    public async Task<IActionResult> ConfirmPayment(Guid id, Guid participantId)
    {
        try
        {
            var result = await _eventService.ConfirmPaymentAsync(id, participantId, GetUserId());
            await _hubContext.Clients.Group(id.ToString()).SendAsync("PaymentConfirmed", result);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
    }

    [HttpPost("{id}/expenses")]
    public async Task<IActionResult> AddExpense(Guid id, [FromBody] CreateExpenseRequest request)
    {
        try
        {
            var result = await _eventService.AddExpenseAsync(id, request, GetUserId());
            await _hubContext.Clients.Group(id.ToString()).SendAsync("ExpenseAdded", result);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{id}/qr")]
    public IActionResult GenerateQR(Guid id, [FromQuery] decimal amount, [FromQuery] string? content,
        [FromQuery] string? bankCode, [FromQuery] string? accountNumber, [FromQuery] string? accountName)
    {
        string qrContent;

        if (!string.IsNullOrEmpty(bankCode) && !string.IsNullOrEmpty(accountNumber))
        {
            // Generate VietQR EMVCo format
            qrContent = BuildVietQRString(bankCode, accountNumber, amount, content ?? $"ES {id.ToString()[..8]}");
        }
        else
        {
            qrContent = content ?? $"EVENT_{id}";
        }

        var qrGenerator = new QRCoder.QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(qrContent, QRCoder.QRCodeGenerator.ECCLevel.Q);
        var qrCode = new QRCoder.PngByteQRCode(qrData);
        var png = qrCode.GetGraphic(10);
        return File(png, "image/png");
    }

    /// <summary>
    /// Builds a VietQR EMVCo-compliant QR string.
    /// Ref: https://www.emvco.com/emv-technologies/qrcodes/
    /// </summary>
    private static string BuildVietQRString(string bankCode, string accountNumber, decimal amount, string addendumData)
    {
        // Bank BIN lookup (common Vietnamese banks)
        var bankBins = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["VCB"] = "970436", ["TCB"] = "970407", ["MB"] = "970422",
            ["ACB"] = "970416", ["TPB"] = "970423", ["VPB"] = "970432",
            ["BIDV"] = "970418", ["VTB"] = "970415", ["SCB"] = "970429",
            ["SHB"] = "970443", ["MSB"] = "970426", ["HDBank"] = "970437",
            ["OCB"] = "970448", ["STB"] = "970403", ["EIB"] = "970431",
            ["ABB"] = "970425", ["NAB"] = "970428", ["LPB"] = "970449",
            ["BAB"] = "970409", ["NCB"] = "970419", ["PGB"] = "970430",
            ["VIB"] = "970441", ["SEAB"] = "970440", ["CAKE"] = "546034",
            ["Ubank"] = "546035", ["TIMO"] = "963388", ["VNPTMONEY"] = "971011",
            ["VIETTEL"] = "971005", ["MOMO"] = "971003"
        };

        var bankBin = bankBins.TryGetValue(bankCode, out var bin) ? bin : bankCode;

        // EMVCo QR format - field construction
        static string tlv(string tag, string value) => $"{tag}{value.Length:D2}{value}";

        // Merchant Account Information (ID 38) - NAPAS
        var guid = "A000000727";  // NAPAS application identifier
        var merchantAccInfo =
            tlv("00", guid) +
            tlv("01", bankBin) +
            tlv("02", accountNumber);

        var payload =
            tlv("00", "01") +                     // Payload Format Indicator
            tlv("01", "11") +                     // Point of Initiation (11 = static, 12 = dynamic)
            tlv("38", merchantAccInfo) +           // Merchant Account Information (NAPAS)
            tlv("52", "5999") +                    // Merchant Category Code
            tlv("53", "704") +                     // Transaction Currency (VND = 704)
            (amount > 0 ? tlv("54", ((long)amount).ToString()) : "") + // Transaction Amount
            tlv("58", "VN") +                      // Country Code
            tlv("62", tlv("08", addendumData));    // Additional Data (Purpose of Transaction)

        // CRC16 calculation (CRC-CCITT)
        var crcInput = payload + "6304";
        var crc = CalculateCRC16(crcInput);
        payload += $"6304{crc:X4}";

        return payload;
    }

    private static ushort CalculateCRC16(string input)
    {
        ushort crc = 0xFFFF;
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        foreach (var b in bytes)
        {
            crc ^= (ushort)(b << 8);
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc = (ushort)(crc << 1);
            }
        }
        return crc;
    }
}
