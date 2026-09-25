# Multi-Entity Consolidation Engine

A financial consolidation engine built in C# / ASP.NET Core that takes trial balances from five subsidiaries across four currencies and produces consolidated group financial statements, following IAS 21 FX translation rules.

## What it does

```
TechCorp India (INR) ─ parent ──────────────┐
TechCorp US (USD) ─ 100% owned ─────────────┤
TechCorp UK (GBP) ─ 80% owned (20% NCI) ───┤──→ Consolidated
TechCorp Germany (EUR) ─ 100% owned ────────┤    Financial
TechCorp Japan (JPY) ─ 75% owned (25% NCI) ─┘    Statements
```

### Consolidation steps

1. **FX translation** — balance-sheet items at the closing rate, P&L items at the period-average rate. The difference between the two is booked as a Currency Translation Adjustment (CTA) in equity.

2. **Intercompany eliminations** — removes internal revenue/expense and receivable/payable balances so the group doesn't double-count activity between its own entities.

3. **Investment elimination** — the parent's "Investment in Subsidiaries" asset is eliminated against the subsidiaries' share capital.

4. **Non-controlling interest (NCI)** — calculates the minority shareholders' share of each partially-owned subsidiary's net assets.

5. **Validation** — five automated checks confirm the output is consistent:
   - Balance sheet balances (A = L + E)
   - Eliminations net to zero
   - Pre-elimination trial balance sums near zero
   - NCI is non-negative
   - No orphan (zero-value) accounts

## Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run

```bash
git clone <your-repo-url>
cd consolidation-engine

dotnet restore
dotnet run
```

Open [http://localhost:5000](http://localhost:5000) (or the port shown in the terminal).

1. Click **Seed data** to populate the database with 5 subsidiaries, FX rates, and intercompany transactions.
2. Click **Consolidate** to run the full workflow.
3. Review the consolidated balance sheet, income statement, eliminations, and validation results.

## API endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/seed` | Reset and seed the database |
| `GET` | `/api/subsidiaries` | List subsidiaries with TB summaries |
| `GET` | `/api/trial-balance/{id}` | One subsidiary's trial balance |
| `GET` | `/api/translations` | FX translation detail per entity |
| `POST` | `/api/consolidate` | Run consolidation, return full result |
| `GET` | `/api/exchange-rates` | FX rates used |
| `GET` | `/api/intercompany` | Intercompany transactions |

## Tech stack

- **Language:** C# / .NET 8
- **Framework:** ASP.NET Core Web API
- **ORM:** Entity Framework Core
- **Database:** SQLite (swap to SQL Server or PostgreSQL by changing the connection string)
- **Frontend:** Vanilla HTML/CSS/JS

## Switching to SQL Server

```csharp
// In Program.cs, replace:
options.UseSqlite("Data Source=consolidation.db");
// With:
options.UseSqlServer("Server=localhost;Database=Consolidation;Trusted_Connection=True;");
```

Add the `Microsoft.EntityFrameworkCore.SqlServer` NuGet package.

## Project structure

```
Controllers/
  ConsolidationController.cs   — REST API endpoints

Models/
  Domain.cs                    — EF Core entities (Subsidiary, TrialBalanceLine, etc.)
  Dtos.cs                      — API response shapes

Services/
  FxTranslationService.cs      — IAS 21 currency translation
  EliminationService.cs        — intercompany and investment eliminations
  ValidationService.cs         — 5 integrity checks
  ConsolidationService.cs      — orchestrates the full workflow

Data/
  AppDbContext.cs               — EF Core context
  SeedData.cs                   — 5 subsidiaries with realistic trial balances
```

## License

MIT
