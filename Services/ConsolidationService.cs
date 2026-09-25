using ConsolidationEngine.Data;
using ConsolidationEngine.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsolidationEngine.Services;

/// <summary>
/// Orchestrates the full consolidation workflow:
///   1. Translate every subsidiary's TB into INR
///   2. Aggregate all translated lines by account
///   3. Apply intercompany eliminations
///   4. Compute Currency Translation Adjustment (CTA)
///   5. Compute Non-Controlling Interest (NCI)
///   6. Run validation checks
/// </summary>
public class ConsolidationService
{
    private readonly AppDbContext _db;
    private readonly FxTranslationService _fx;
    private readonly EliminationService _elim;
    private readonly ValidationService _validation;

    public ConsolidationService(
        AppDbContext db,
        FxTranslationService fx,
        EliminationService elim,
        ValidationService validation)
    {
        _db = db;
        _fx = fx;
        _elim = elim;
        _validation = validation;
    }

    public ConsolidationResult Consolidate()
    {
        // ── Step 1: Translate all subsidiaries ──
        var translations = _fx.TranslateAll();

        // ── Step 2: Aggregate by account ──
        var aggregated = new Dictionary<string, ConsolidatedLine>();

        foreach (var tr in translations)
        {
            foreach (var line in tr.Lines)
            {
                var key = line.AccountCode;
                if (!aggregated.ContainsKey(key))
                {
                    aggregated[key] = new ConsolidatedLine
                    {
                        AccountCode = line.AccountCode,
                        AccountName = line.AccountName,
                        AccountType = line.AccountType,
                        PreElimination = 0,
                        EliminationAdj = 0,
                        Consolidated = 0,
                    };
                }
                aggregated[key].PreElimination += line.TranslatedAmount;
            }
        }

        // ── Step 3: Apply eliminations ──
        var eliminations = _elim.GenerateEliminations();

        foreach (var entry in eliminations)
        {
            // Debit side: if the account is normally a credit (revenue,
            // liability, equity), a debit reduces it → positive adjustment.
            // If normally a debit (asset, expense), a debit increases it.
            if (aggregated.ContainsKey(entry.DebitAccount))
            {
                var line = aggregated[entry.DebitAccount];
                // Debit the account: for revenue/liability/equity (stored
                // as negative), this adds a positive amount (reduces the
                // absolute value).  For asset/expense (stored as positive),
                // this also adds (increases).
                // In elimination entries, we always want to REVERSE the
                // intercompany balance, so:
                //   Debit revenue → +amount (cancels the negative revenue)
                //   Debit payable → +amount (cancels the negative liability)
                line.EliminationAdj += entry.Amount;
            }

            // Credit side: opposite effect.
            if (aggregated.ContainsKey(entry.CreditAccount))
            {
                var line = aggregated[entry.CreditAccount];
                line.EliminationAdj -= entry.Amount;
            }
        }

        // compute consolidated = pre-elimination + elimination adjustment
        foreach (var line in aggregated.Values)
        {
            line.Consolidated = line.PreElimination + line.EliminationAdj;
        }

        // ── Step 4: Currency Translation Reserve ──
        decimal cta = translations.Sum(t => t.CurrencyTranslationAdjustment);

        // ── Step 5: Non-Controlling Interest ──
        // NCI = minority share of each subsidiary's net assets (equity)
        var subs = _db.Subsidiaries
            .Include(s => s.TrialBalance)
            .Where(s => !s.IsParent && s.OwnershipPct < 100)
            .ToList();

        decimal totalNci = 0;
        foreach (var sub in subs)
        {
            var tr = translations.First(t => t.SubsidiaryName == sub.Name);

            // net assets = total equity of this subsidiary in INR
            decimal subEquity = tr.Lines
                .Where(l => l.AccountType == AccountType.Equity)
                .Sum(l => l.TranslatedAmount);

            // NCI gets the minority share (as a positive number)
            decimal minorityPct = (100m - sub.OwnershipPct) / 100m;
            totalNci += Math.Abs(subEquity) * minorityPct;
        }
        totalNci = Math.Round(totalNci, 2);

        // ── Step 6: Split into BS and P&L ──
        var bs = aggregated.Values
            .Where(l => l.AccountType is AccountType.Asset
                                      or AccountType.Liability
                                      or AccountType.Equity)
            .OrderBy(l => l.AccountCode)
            .ToList();

        var pnl = aggregated.Values
            .Where(l => l.AccountType is AccountType.Revenue
                                      or AccountType.Expense)
            .OrderBy(l => l.AccountCode)
            .ToList();

        var result = new ConsolidationResult
        {
            BalanceSheet = bs,
            IncomeStatement = pnl,
            CurrencyTranslationReserve = Math.Round(cta, 2),
            NonControllingInterest = totalNci,
            Eliminations = eliminations,
        };

        // ── Step 7: Validate ──
        result.Validations = _validation.Validate(result);

        // ── Summary ──
        result.Summary = new ConsolidationSummary
        {
            SubsidiariesConsolidated = translations.Count,
            AccountsProcessed = aggregated.Count,
            EliminationsApplied = eliminations.Count,
            TotalAssets = bs
                .Where(l => l.AccountType == AccountType.Asset)
                .Sum(l => l.Consolidated),
            TotalLiabilities = bs
                .Where(l => l.AccountType == AccountType.Liability)
                .Sum(l => l.Consolidated),
            TotalEquity = bs
                .Where(l => l.AccountType == AccountType.Equity)
                .Sum(l => l.Consolidated),
            TotalRevenue = pnl
                .Where(l => l.AccountType == AccountType.Revenue)
                .Sum(l => l.Consolidated),
            NetIncome = pnl.Sum(l => l.Consolidated),
            CurrencyTranslationReserve = Math.Round(cta, 2),
            NonControllingInterest = totalNci,
            ValidationsPassed = result.Validations.Count(v => v.Passed),
            ValidationsFailed = result.Validations.Count(v => !v.Passed),
        };

        return result;
    }
}
