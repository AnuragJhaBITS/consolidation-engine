using ConsolidationEngine.Models;

namespace ConsolidationEngine.Services;

/// <summary>
/// Runs integrity checks on the consolidated output.
/// Every check returns pass/fail with a detail message.
/// </summary>
public class ValidationService
{
    /// <summary>
    /// Run all validation rules against a consolidation result.
    /// </summary>
    public List<ValidationCheck> Validate(ConsolidationResult result)
    {
        var checks = new List<ValidationCheck>();

        checks.Add(CheckBalanceSheetBalances(result));
        checks.Add(CheckEliminationsNetToZero(result));
        checks.Add(CheckTrialBalanceTotals(result));
        checks.Add(CheckNciIsPositive(result));
        checks.Add(CheckNoOrphanAccounts(result));

        return checks;
    }

    /// <summary>
    /// Assets = Liabilities + Equity (including CTA and NCI).
    /// The accounting equation must hold after consolidation.
    /// </summary>
    private ValidationCheck CheckBalanceSheetBalances(ConsolidationResult result)
    {
        decimal totalAssets = result.BalanceSheet
            .Where(l => l.AccountType == AccountType.Asset)
            .Sum(l => l.Consolidated);

        // liabilities and equity are stored as negative (credit),
        // so their sum should equal negative of assets
        decimal totalLiabEquity = result.BalanceSheet
            .Where(l => l.AccountType is AccountType.Liability or AccountType.Equity)
            .Sum(l => l.Consolidated);

        // add CTA and NCI (both are equity-side items, stored as credits)
        decimal equitySide = totalLiabEquity
                           - result.CurrencyTranslationReserve
                           - result.NonControllingInterest;

        decimal imbalance = totalAssets + equitySide;
        bool passed = Math.Abs(imbalance) < 1m; // ₹1 tolerance for rounding

        return new ValidationCheck
        {
            Rule = "Balance sheet balances (A = L + E)",
            Passed = passed,
            Detail = passed
                ? $"Assets {totalAssets:N0} = Liabilities + Equity {-equitySide:N0}. Balanced."
                : $"Imbalance of {imbalance:N2} detected. Assets={totalAssets:N0}, L+E={-equitySide:N0}.",
        };
    }

    /// <summary>
    /// Every elimination entry has an equal debit and credit,
    /// so the net impact on the trial balance should be zero.
    /// </summary>
    private ValidationCheck CheckEliminationsNetToZero(ConsolidationResult result)
    {
        // Each elimination entry is a debit-credit pair of the same amount,
        // so by construction they net to zero.  But we verify the consolidated
        // lines' elimination adjustments sum to zero.
        decimal netElim = result.BalanceSheet
            .Concat(result.IncomeStatement)
            .Sum(l => l.EliminationAdj);

        bool passed = Math.Abs(netElim) < 1m;

        return new ValidationCheck
        {
            Rule = "Eliminations net to zero",
            Passed = passed,
            Detail = passed
                ? $"Net elimination impact: {netElim:N2}. Balanced."
                : $"Eliminations do not net to zero: {netElim:N2}.",
        };
    }

    /// <summary>
    /// The pre-elimination trial balance (sum of all translated lines)
    /// should itself have debits ≈ credits for each subsidiary.
    /// </summary>
    private ValidationCheck CheckTrialBalanceTotals(ConsolidationResult result)
    {
        decimal totalPreElim = result.BalanceSheet
            .Concat(result.IncomeStatement)
            .Sum(l => l.PreElimination);

        bool passed = Math.Abs(totalPreElim) < 100m; // wider tolerance (rounding across entities)

        return new ValidationCheck
        {
            Rule = "Pre-elimination trial balance sums to near zero",
            Passed = passed,
            Detail = passed
                ? $"Sum of all pre-elimination lines: {totalPreElim:N2}."
                : $"Pre-elimination total is {totalPreElim:N2} (expected near zero).",
        };
    }

    /// <summary>
    /// Non-controlling interest should be non-negative (minority
    /// shareholders can't owe the group money under normal operations).
    /// </summary>
    private ValidationCheck CheckNciIsPositive(ConsolidationResult result)
    {
        bool passed = result.NonControllingInterest >= 0;

        return new ValidationCheck
        {
            Rule = "Non-controlling interest is non-negative",
            Passed = passed,
            Detail = $"NCI: {result.NonControllingInterest:N2}.",
        };
    }

    /// <summary>
    /// Every consolidated line should have at least one subsidiary
    /// contributing to it (no phantom accounts).
    /// </summary>
    private ValidationCheck CheckNoOrphanAccounts(ConsolidationResult result)
    {
        var orphans = result.BalanceSheet
            .Concat(result.IncomeStatement)
            .Where(l => l.PreElimination == 0 && l.EliminationAdj == 0 && l.Consolidated == 0)
            .ToList();

        bool passed = orphans.Count == 0;

        return new ValidationCheck
        {
            Rule = "No orphan (zero-value) accounts",
            Passed = passed,
            Detail = passed
                ? "All consolidated accounts have non-zero activity."
                : $"{orphans.Count} account(s) have zero across all columns.",
        };
    }
}
