using System.Security.Claims;
using Cilt_Beninden_Kanser_Api.Application.UseCases.GetAnalysisHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cilt_Beninden_Kanser_Api.WebAPI.Controllers;

[ApiController]
[Route("api/analysis")]
[Authorize]
public class HistoryController : ControllerBase
{
    private readonly GetHistoryHandler _handler;

    public HistoryController(GetHistoryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized("Geçmişi görüntülemek için giriş yapmanız gerekir.");

        var query = new GetHistoryQuery(userId.Value);
        var results = await _handler.HandleAsync(query, ct);
        return Ok(results);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null ? Guid.Parse(claim.Value) : null;
    }
}
