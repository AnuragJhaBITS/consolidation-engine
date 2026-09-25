using ConsolidationEngine.Data;
using ConsolidationEngine.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsolidationEngine.Services;

/// <summary>
/// Translates each foreign subsidiary's trial balance into the group
/// reporting currency (INR).
///
/// IAS 21 rules:
///   Balance-sheet items (assets, liabilities, equity) → closing rate
///   P&L items (revenue, expenses)                    → average rate
///   The difference between the two approaches is booked as a
///   Currency Translation Adjustment (CTA) in equity.
///
/// The parent's books are already in INR, so no translation needed.
/// </summary>
public class FxTranslationService
{
    private readonly AppDbContext _db;

    public FxTranslationService(AppDbContext db) => _db = db;

    /// <summary>
    /// Translate one subsidiary's TB lines to INR.
    /// Returns the translated lines and the CTA.
    /// </summary>
    public TranslationResult Translate(Subsidiary subsidiary)
    {
        if (subsidiary.IsParent)
        {
            // parent is already in reporting currency
            return new TranslationResult
            {
                SubsidiaryName = subsidiary.Name,
                FromCurrency = subsidiary.Currency,
                ClosingRate = 1m,
                AverageRate = 1m,
                Lines = subsidiary.TrialBalance.Select(line => new TranslatedLine
                {
                    AccountCode = line.AccountCode,
                    AccountName = line.AccountName,
                    AccountType = line.AccountType,
                    LocalAmount = line.Amount,
                    RateApplied = 1m,
                    TranslatedAmount = line.Amount,
                }).ToList(),
                CurrencyTranslationAdjustment = 0m,
            };
        }

        var rate = _db.ExchangeRates
            .FirstOrDefault(r => r.FromCurrency == subsidiary.Currency
                              && r.ToCurrency == "INR");

        if (rate == null)
            throw new InvalidOperationException(
                $"No FX rate found for {subsidiary.Currency} → INR");

        var lines = new List<TranslatedLine>();

        // translate each line using the appropriate rate
        decimal totalAtClosing = 0;
        decimal totalAtAverage = 0;

        foreach (var line in subsidiary.TrialBalance)
        {
            decimal appliedRate;
            if (line.AccountType is AccountType.Revenue or AccountType.Expense)
            {
                // P&L → average rate
                appliedRate = rate.AverageRate;
            }
            else
            {
                // Balance sheet → closing rate
                appliedRate = rate.ClosingRate;
            }

            decimal translated = Math.Round(line.Amount * appliedRate, 2);
            lines.Add(new TranslatedLine
            {
                AccountCode = line.AccountCode,
                AccountName = line.AccountName,
                AccountType = line.AccountType,
                LocalAmount = line.Amount,
                RateApplied = appliedRate,
                TranslatedAmount = translated,
            });

            // for CTA calculation: what is the difference between
            // translating everything at closing vs the mixed approach?
            totalAtClosing += Math.Round(line.Amount * rate.ClosingRate, 2);
        }

        decimal totalTranslated = lines.Sum(l => l.TranslatedAmount);

        // CTA = sum(all at closing) − sum(mixed translation)
        // This is the "plug" that makes the translated balance sheet balance.
        decimal cta = totalAtClosing - totalTranslated;

        return new TranslationResult
        {
            SubsidiaryName = subsidiary.Name,
            FromCurrency = subsidiary.Currency,
            ClosingRate = rate.ClosingRate,
            AverageRate = rate.AverageRate,
            Lines = lines,
            CurrencyTranslationAdjustment = Math.Round(cta, 2),
        };
    }

    /// <summary>
    /// Translate all foreign subsidiaries and return per-entity results.
    /// </summary>
    public List<TranslationResult> TranslateAll()
    {
        var subs = _db.Subsidiaries
            .Include(s => s.TrialBalance)
            .ToList();

        return subs.Select(Translate).ToList();
    }
}
