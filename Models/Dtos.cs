namespace ConsolidationEngine.Models;

// ── Consolidation result ──

public class ConsolidationResult
{
    public List<ConsolidatedLine> BalanceSheet { get; set; } = new();
    public List<ConsolidatedLine> IncomeStatement { get; set; } = new();
    public decimal CurrencyTranslationReserve { get; set; }
    public decimal NonControllingInterest { get; set; }
    public List<EliminationEntry> Eliminations { get; set; } = new();
    public List<ValidationCheck> Validations { get; set; } = new();
    public ConsolidationSummary Summary { get; set; } = new();
}

public class ConsolidatedLine
{
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public AccountType AccountType { get; set; }

    /// <summary>Sum of all subsidiaries before eliminations, in reporting currency.</summary>
    public decimal PreElimination { get; set; }

    /// <summary>Elimination adjustment.</summary>
    public decimal EliminationAdj { get; set; }

    /// <summary>Final consolidated amount.</summary>
    public decimal Consolidated { get; set; }
}

public class EliminationEntry
{
    public string Description { get; set; } = "";
    public string DebitAccount { get; set; } = "";
    public string CreditAccount { get; set; } = "";
    public decimal Amount { get; set; }
}

public class ValidationCheck
{
    public string Rule { get; set; } = "";
    public bool Passed { get; set; }
    public string Detail { get; set; } = "";
}

public class ConsolidationSummary
{
    public int SubsidiariesConsolidated { get; set; }
    public int AccountsProcessed { get; set; }
    public int EliminationsApplied { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal NetIncome { get; set; }
    public decimal CurrencyTranslationReserve { get; set; }
    public decimal NonControllingInterest { get; set; }
    public int ValidationsPassed { get; set; }
    public int ValidationsFailed { get; set; }
}

// ── Subsidiary detail ──

public class SubsidiaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Currency { get; set; } = "";
    public decimal OwnershipPct { get; set; }
    public bool IsParent { get; set; }
    public int AccountCount { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
}

// ── Translation detail ──

public class TranslationResult
{
    public string SubsidiaryName { get; set; } = "";
    public string FromCurrency { get; set; } = "";
    public decimal ClosingRate { get; set; }
    public decimal AverageRate { get; set; }
    public List<TranslatedLine> Lines { get; set; } = new();
    public decimal CurrencyTranslationAdjustment { get; set; }
}

public class TranslatedLine
{
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public AccountType AccountType { get; set; }
    public decimal LocalAmount { get; set; }
    public decimal RateApplied { get; set; }
    public decimal TranslatedAmount { get; set; }
}
