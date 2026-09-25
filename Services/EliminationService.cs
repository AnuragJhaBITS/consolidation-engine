using ConsolidationEngine.Data;
using ConsolidationEngine.Models;

namespace ConsolidationEngine.Services;

/// <summary>
/// Generates elimination journal entries that remove intercompany
/// activity from the consolidated statements.
///
/// Two types of eliminations:
///   1. Revenue / expense: the seller's intercompany revenue and the
///      buyer's intercompany expense cancel out.
///   2. Receivable / payable: the seller's intercompany receivable and
///      the buyer's intercompany payable cancel out.
///
/// Additionally, the parent's "Investment in Subsidiaries" asset is
/// eliminated against the subsidiaries' share capital (the equity
/// elimination entry).
/// </summary>
public class EliminationService
{
    private readonly AppDbContext _db;

    public EliminationService(AppDbContext db) => _db = db;

    public List<EliminationEntry> GenerateEliminations()
    {
        var entries = new List<EliminationEntry>();

        // ── 1. Intercompany revenue/expense and receivable/payable ──

        var icTxns = _db.IntercompanyTransactions.ToList();

        foreach (var txn in icTxns)
        {
            // Eliminate revenue vs expense
            entries.Add(new EliminationEntry
            {
                Description = $"Eliminate IC revenue/expense: {txn.Description}",
                DebitAccount = txn.RevenueAccount,   // debit revenue (reduces it)
                CreditAccount = txn.ExpenseAccount,  // credit expense (reduces it)
                Amount = txn.Amount,
            });

            // Eliminate receivable vs payable
            entries.Add(new EliminationEntry
            {
                Description = $"Eliminate IC receivable/payable: {txn.Description}",
                DebitAccount = txn.PayableAccount,    // debit payable (reduces it)
                CreditAccount = txn.ReceivableAccount, // credit receivable (reduces it)
                Amount = txn.Amount,
            });
        }

        // ── 2. Investment elimination ──
        // Parent's "Investment in Subsidiaries" is eliminated against
        // each subsidiary's share capital (proportional to ownership).

        var parent = _db.Subsidiaries.First(s => s.IsParent);
        var investmentLine = _db.TrialBalanceLines
            .FirstOrDefault(l => l.SubsidiaryId == parent.Id
                              && l.AccountCode == "1400");

        if (investmentLine != null)
        {
            entries.Add(new EliminationEntry
            {
                Description = "Eliminate Investment in Subsidiaries vs subsidiary share capital",
                DebitAccount = "3000",   // debit share capital (reduces equity)
                CreditAccount = "1400",  // credit investment (reduces asset)
                Amount = Math.Abs(investmentLine.Amount),
            });
        }

        return entries;
    }
}
