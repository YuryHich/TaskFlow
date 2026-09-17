using Application.DTOs.Audit;
using Application.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly IAuditLogService _auditLogs;

    public AuditController(IAuditLogService auditLogs)
    {
        _auditLogs = auditLogs;
    }

    [HttpGet]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Manager)}")]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetRecent([FromQuery] int take = 50)
    {
        return Ok(await _auditLogs.GetRecentAsync(take));
    }
}
