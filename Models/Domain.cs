namespace ConsolidationEngine.Models;

/// <summary>
/// A legal entity within the group. Each subsidiary keeps its books
/// in its own functional currency.
/// </summary>
public class Subsidiary
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Currency { get; set; } = "";

    /// <summary>Parent's ownership percentage (0–100).</summary>
    public decimal OwnershipPct { get; set; }

    /// <summary>True for the parent entity (reporting currency).</summary>
    public bool IsParent { get; set; }

    public List<TrialBalanceLine> TrialBalance { get; set; } = new();
}

/// <summary>
/// One line of a subsidiary's trial balance.
/// Accounts are classified so the engine knows which FX rate to apply:
///   - Balance-sheet accounts (Asset, Liability, Equity) → closing rate
///   - P&L accounts (Revenue, Expense) → average rate
/// </summary>
public class TrialBalanceLine
{
    public int Id { get; set; }
    public int SubsidiaryId { get; set; }
    public Subsidiary? Subsidiary { get; set; }

    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public AccountType AccountType { get; set; }

    /// <summary>Amount in the subsidiary's functional currency.
    /// Positive = debit (assets, expenses). Negative = credit (liabilities, equity, revenue).</summary>
    public decimal Amount { get; set; }
}

public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense
}

/// <summary>
/// FX rates for translating a foreign subsidiary to the group
/// reporting currency.
/// </summary>
public class ExchangeRate
{
    public int Id { get; set; }

    /// <summary>Foreign currency code (e.g. "USD").</summary>
    public string FromCurrency { get; set; } = "";

    /// <summary>Group reporting currency (e.g. "INR").</summary>
    public string ToCurrency { get; set; } = "";

    /// <summary>Spot rate at period end. Used for balance-sheet items.</summary>
    public decimal ClosingRate { get; set; }

    /// <summary>Average rate over the period. Used for P&L items.</summary>
    public decimal AverageRate { get; set; }
}

/// <summary>
/// A transaction between two group entities that must be eliminated
/// during consolidation so the group doesn't count internal activity
/// as real revenue or debt.
/// </summary>
public class IntercompanyTransaction
{
    public int Id { get; set; }
    public int FromSubsidiaryId { get; set; }
    public Subsidiary? FromSubsidiary { get; set; }
    public int ToSubsidiaryId { get; set; }
    public Subsidiary? ToSubsidiary { get; set; }

    /// <summary>Description of the intercompany transaction.</summary>
    public string Description { get; set; } = "";

    /// <summary>Amount in the reporting currency (INR).</summary>
    public decimal Amount { get; set; }

    /// <summary>Revenue/expense account to eliminate on the seller side.</summary>
    public string RevenueAccount { get; set; } = "";

    /// <summary>Expense/asset account to eliminate on the buyer side.</summary>
    public string ExpenseAccount { get; set; } = "";

    /// <summary>Receivable account on seller's books.</summary>
    public string ReceivableAccount { get; set; } = "";

    /// <summary>Payable account on buyer's books.</summary>
    public string PayableAccount { get; set; } = "";
}
