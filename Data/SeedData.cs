using ConsolidationEngine.Models;

namespace ConsolidationEngine.Data;

/// <summary>
/// Seeds 5 subsidiaries across 4 currencies with realistic trial balances,
/// FX rates, and intercompany transactions.
///
/// Group structure:
///   TechCorp India (INR) — parent, 100%
///   TechCorp US (USD)    — wholly owned, 100%
///   TechCorp UK (GBP)    — 80% owned → 20% NCI
///   TechCorp Germany (EUR) — wholly owned, 100%
///   TechCorp Japan (JPY) — 75% owned → 25% NCI
///
/// Reporting currency: INR
/// Period: FY 2024-25 (April 2024 – March 2025)
/// </summary>
public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        // ── Subsidiaries ──

        var india = new Subsidiary
        {
            Name = "TechCorp India",
            Currency = "INR",
            OwnershipPct = 100,
            IsParent = true
        };
        var us = new Subsidiary
        {
            Name = "TechCorp US",
            Currency = "USD",
            OwnershipPct = 100,
            IsParent = false
        };
        var uk = new Subsidiary
        {
            Name = "TechCorp UK",
            Currency = "GBP",
            OwnershipPct = 80,
            IsParent = false
        };
        var germany = new Subsidiary
        {
            Name = "TechCorp Germany",
            Currency = "EUR",
            OwnershipPct = 100,
            IsParent = false
        };
        var japan = new Subsidiary
        {
            Name = "TechCorp Japan",
            Currency = "JPY",
            OwnershipPct = 75,
            IsParent = false
        };

        db.Subsidiaries.AddRange(india, us, uk, germany, japan);
        db.SaveChanges();

        // ── Exchange rates (to INR) ──
        // Parent is INR, so no rate needed for India.

        db.ExchangeRates.AddRange(
            new ExchangeRate
            {
                FromCurrency = "USD",
                ToCurrency = "INR",
                ClosingRate = 83.50m,
                AverageRate = 83.00m
            },
            new ExchangeRate
            {
                FromCurrency = "GBP",
                ToCurrency = "INR",
                ClosingRate = 106.20m,
                AverageRate = 105.50m
            },
            new ExchangeRate
            {
                FromCurrency = "EUR",
                ToCurrency = "INR",
                ClosingRate = 91.00m,
                AverageRate = 90.50m
            },
            new ExchangeRate
            {
                FromCurrency = "JPY",
                ToCurrency = "INR",
                ClosingRate = 0.56m,
                AverageRate = 0.55m
            }
        );
        db.SaveChanges();

        // ── Trial balances ──
        // Convention: positive = debit, negative = credit

        // --- India (INR) ---
        AddTB(db, india.Id, new[]
        {
            ("1000", "Cash & Bank",              AccountType.Asset,     4_50_00_000m),
            ("1100", "Accounts Receivable",       AccountType.Asset,     3_20_00_000m),
            ("1200", "Intercompany Receivable",   AccountType.Asset,       60_00_000m),
            ("1300", "Fixed Assets (Net)",        AccountType.Asset,     8_00_00_000m),
            ("1400", "Investment in Subsidiaries", AccountType.Asset,   12_00_00_000m),
            ("2000", "Accounts Payable",          AccountType.Liability, -2_10_00_000m),
            ("2100", "Short-Term Borrowings",     AccountType.Liability, -3_00_00_000m),
            ("2200", "Long-Term Debt",            AccountType.Liability, -5_00_00_000m),
            ("3000", "Share Capital",             AccountType.Equity,    -8_00_00_000m),
            ("3100", "Retained Earnings",         AccountType.Equity,    -6_20_00_000m),
            ("4000", "Service Revenue",           AccountType.Revenue,  -12_00_00_000m),
            ("4100", "Intercompany Revenue",      AccountType.Revenue,     -60_00_000m),
            ("5000", "Employee Costs",            AccountType.Expense,    5_50_00_000m),
            ("5100", "Technology & Infra",         AccountType.Expense,    1_80_00_000m),
            ("5200", "Rent & Utilities",          AccountType.Expense,      70_00_000m),
            ("5300", "Depreciation",              AccountType.Expense,      60_00_000m),
        });

        // --- US (USD) ---
        AddTB(db, us.Id, new[]
        {
            ("1000", "Cash & Bank",              AccountType.Asset,       850_000m),
            ("1100", "Accounts Receivable",       AccountType.Asset,       620_000m),
            ("1300", "Fixed Assets (Net)",        AccountType.Asset,     1_200_000m),
            ("2000", "Accounts Payable",          AccountType.Liability,  -380_000m),
            ("2100", "Intercompany Payable",      AccountType.Liability,   -72_000m),
            ("2200", "Long-Term Debt",            AccountType.Liability,  -800_000m),
            ("3000", "Share Capital",             AccountType.Equity,     -500_000m),
            ("3100", "Retained Earnings",         AccountType.Equity,     -418_000m),
            ("4000", "Service Revenue",           AccountType.Revenue,  -2_400_000m),
            ("5000", "Employee Costs",            AccountType.Expense,   1_100_000m),
            ("5100", "Technology & Infra",         AccountType.Expense,     320_000m),
            ("5200", "Rent & Utilities",          AccountType.Expense,     180_000m),
            ("5300", "Depreciation",              AccountType.Expense,     100_000m),
            ("5400", "Intercompany Expense",      AccountType.Expense,      72_000m),
            ("5500", "Marketing",                 AccountType.Expense,     128_000m),
        });

        // --- UK (GBP) ---
        AddTB(db, uk.Id, new[]
        {
            ("1000", "Cash & Bank",              AccountType.Asset,       420_000m),
            ("1100", "Accounts Receivable",       AccountType.Asset,       310_000m),
            ("1300", "Fixed Assets (Net)",        AccountType.Asset,       680_000m),
            ("2000", "Accounts Payable",          AccountType.Liability,  -195_000m),
            ("2200", "Long-Term Debt",            AccountType.Liability,  -400_000m),
            ("3000", "Share Capital",             AccountType.Equity,     -300_000m),
            ("3100", "Retained Earnings",         AccountType.Equity,     -215_000m),
            ("4000", "Service Revenue",           AccountType.Revenue,  -1_200_000m),
            ("5000", "Employee Costs",            AccountType.Expense,     520_000m),
            ("5100", "Technology & Infra",         AccountType.Expense,     160_000m),
            ("5200", "Rent & Utilities",          AccountType.Expense,      95_000m),
            ("5300", "Depreciation",              AccountType.Expense,      50_000m),
            ("5500", "Marketing",                 AccountType.Expense,      75_000m),
        });

        // --- Germany (EUR) ---
        AddTB(db, germany.Id, new[]
        {
            ("1000", "Cash & Bank",              AccountType.Asset,       380_000m),
            ("1100", "Accounts Receivable",       AccountType.Asset,       270_000m),
            ("1200", "Intercompany Receivable",   AccountType.Asset,        45_000m),
            ("1300", "Fixed Assets (Net)",        AccountType.Asset,       520_000m),
            ("2000", "Accounts Payable",          AccountType.Liability,  -165_000m),
            ("2100", "Intercompany Payable",      AccountType.Liability,   -45_000m),
            ("2200", "Long-Term Debt",            AccountType.Liability,  -350_000m),
            ("3000", "Share Capital",             AccountType.Equity,     -250_000m),
            ("3100", "Retained Earnings",         AccountType.Equity,     -155_000m),
            ("4000", "Service Revenue",           AccountType.Revenue,    -950_000m),
            ("4100", "Intercompany Revenue",      AccountType.Revenue,     -45_000m),
            ("5000", "Employee Costs",            AccountType.Expense,     420_000m),
            ("5100", "Technology & Infra",         AccountType.Expense,     130_000m),
            ("5200", "Rent & Utilities",          AccountType.Expense,      70_000m),
            ("5300", "Depreciation",              AccountType.Expense,      40_000m),
            ("5500", "Marketing",                 AccountType.Expense,      85_000m),
        });

        // --- Japan (JPY) ---
        AddTB(db, japan.Id, new[]
        {
            ("1000", "Cash & Bank",              AccountType.Asset,    65_000_000m),
            ("1100", "Accounts Receivable",       AccountType.Asset,    48_000_000m),
            ("1300", "Fixed Assets (Net)",        AccountType.Asset,    95_000_000m),
            ("2000", "Accounts Payable",          AccountType.Liability,-28_000_000m),
            ("2200", "Long-Term Debt",            AccountType.Liability,-60_000_000m),
            ("3000", "Share Capital",             AccountType.Equity,  -40_000_000m),
            ("3100", "Retained Earnings",         AccountType.Equity,  -30_000_000m),
            ("4000", "Service Revenue",           AccountType.Revenue,-180_000_000m),
            ("5000", "Employee Costs",            AccountType.Expense,  78_000_000m),
            ("5100", "Technology & Infra",         AccountType.Expense,  22_000_000m),
            ("5200", "Rent & Utilities",          AccountType.Expense,  12_000_000m),
            ("5300", "Depreciation",              AccountType.Expense,   8_000_000m),
            ("5500", "Marketing",                 AccountType.Expense,  10_000_000m),
        });

        // ── Intercompany transactions ──
        // These must be eliminated during consolidation.

        db.IntercompanyTransactions.AddRange(
            // India sold IT services to US for ₹60L (≈ $72k at avg rate)
            new IntercompanyTransaction
            {
                FromSubsidiaryId = india.Id,
                ToSubsidiaryId = us.Id,
                Description = "IT services rendered by India to US",
                Amount = 60_00_000m,
                RevenueAccount = "4100",    // India's intercompany revenue
                ExpenseAccount = "5400",    // US's intercompany expense
                ReceivableAccount = "1200", // India's intercompany receivable
                PayableAccount = "2100",    // US's intercompany payable
            },
            // Germany sold a software license to its own intercompany (internal)
            new IntercompanyTransaction
            {
                FromSubsidiaryId = germany.Id,
                ToSubsidiaryId = germany.Id,
                Description = "Internal intercompany entry (self-balancing)",
                Amount = 45_00_000m,        // ≈ EUR 45k × 100 (already in INR concept)
                RevenueAccount = "4100",
                ExpenseAccount = "5400",
                ReceivableAccount = "1200",
                PayableAccount = "2100",
            }
        );
        db.SaveChanges();
    }

    private static void AddTB(
        AppDbContext db,
        int subsidiaryId,
        (string code, string name, AccountType type, decimal amount)[] lines)
    {
        foreach (var (code, name, type, amount) in lines)
        {
            db.TrialBalanceLines.Add(new TrialBalanceLine
            {
                SubsidiaryId = subsidiaryId,
                AccountCode = code,
                AccountName = name,
                AccountType = type,
                Amount = amount,
            });
        }
        db.SaveChanges();
    }
}
