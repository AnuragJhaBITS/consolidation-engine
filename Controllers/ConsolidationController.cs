using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConsolidationEngine.Data;
using ConsolidationEngine.Models;
using ConsolidationEngine.Services;

namespace ConsolidationEngine.Controllers;

[ApiController]
[Route("api")]
public class ConsolidationController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ConsolidationService _consolidation;
    private readonly FxTranslationService _fx;

    public ConsolidationController(
        AppDbContext db,
        ConsolidationService consolidation,
        FxTranslationService fx)
    {
        _db = db;
        _consolidation = consolidation;
        _fx = fx;
    }

    /// <summary>Reset database and seed fresh data.</summary>
    [HttpPost("seed")]
    public IActionResult Seed()
    {
        SeedData.Initialize(_db);
        return Ok(new { status = "ok", message = "Database seeded with 5 subsidiaries." });
    }

    /// <summary>List all subsidiaries with summary stats.</summary>
    [HttpGet("subsidiaries")]
    public IActionResult GetSubsidiaries()
    {
        var subs = _db.Subsidiaries
            .Include(s => s.TrialBalance)
            .Select(s => new SubsidiaryDto
            {
                Id = s.Id,
                Name = s.Name,
                Currency = s.Currency,
                OwnershipPct = s.OwnershipPct,
                IsParent = s.IsParent,
                AccountCount = s.TrialBalance.Count,
                TotalDebits = s.TrialBalance
                    .Where(l => l.Amount > 0).Sum(l => l.Amount),
                TotalCredits = s.TrialBalance
                    .Where(l => l.Amount < 0).Sum(l => l.Amount),
            })
            .ToList();

        return Ok(subs);
    }

    /// <summary>Get a subsidiary's trial balance.</summary>
    [HttpGet("trial-balance/{subsidiaryId}")]
    public IActionResult GetTrialBalance(int subsidiaryId)
    {
        var sub = _db.Subsidiaries
            .Include(s => s.TrialBalance)
            .FirstOrDefault(s => s.Id == subsidiaryId);

        if (sub == null) return NotFound();

        return Ok(new
        {
            subsidiary = sub.Name,
            currency = sub.Currency,
            lines = sub.TrialBalance
                .OrderBy(l => l.AccountCode)
                .Select(l => new
                {
                    l.AccountCode,
                    l.AccountName,
                    accountType = l.AccountType.ToString(),
                    l.Amount,
                }),
        });
    }

    /// <summary>Show FX translation detail for each subsidiary.</summary>
    [HttpGet("translations")]
    public IActionResult GetTranslations()
    {
        var results = _fx.TranslateAll();
        return Ok(results);
    }

    /// <summary>Run full consolidation and return results.</summary>
    [HttpPost("consolidate")]
    public IActionResult Consolidate()
    {
        var result = _consolidation.Consolidate();
        return Ok(result);
    }

    /// <summary>Get FX rates.</summary>
    [HttpGet("exchange-rates")]
    public IActionResult GetExchangeRates()
    {
        var rates = _db.ExchangeRates.ToList();
        return Ok(rates);
    }

    /// <summary>Get intercompany transactions.</summary>
    [HttpGet("intercompany")]
    public IActionResult GetIntercompany()
    {
        var txns = _db.IntercompanyTransactions
            .Include(t => t.FromSubsidiary)
            .Include(t => t.ToSubsidiary)
            .Select(t => new
            {
                t.Id,
                from = t.FromSubsidiary!.Name,
                to = t.ToSubsidiary!.Name,
                t.Description,
                t.Amount,
                t.RevenueAccount,
                t.ExpenseAccount,
                t.ReceivableAccount,
                t.PayableAccount,
            })
            .ToList();

        return Ok(txns);
    }
}
