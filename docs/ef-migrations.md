# EF Core database migrations

This project uses **EF Core to author and apply the database schema (DDL) via migrations**, while runtime
data access stays on **Dapper (DML)**. The two are deliberately separate:

| Concern | Owner |
|---|---|
| Creating / evolving tables, columns, indexes | EF Core migrations — product: `Tedwren.DataAccess/Ef`; commercial: `Tedwren.DataAccess.Commercial/Ef` (see §8) |
| Reading / writing rows at runtime | Dapper repositories (`Tedwren.DataAccess/Repositories`, `Tedwren.DataAccess.Commercial/Repositories`) |

The EF model lives in `src/Tedwren.DataAccess/Ef`:

- `SchemaRecords.cs` — flat classes that mirror each table 1:1. They exist only to describe the schema and
  are never used at runtime.
- `TedwrenDbContext.cs` — table/column/index mappings. On PostgreSQL every identifier is folded to lower
  case so the Dapper repositories' unquoted SQL resolves to the EF-created tables.
- `TedwrenDbContextFactory.cs` — a design-time factory so the `dotnet ef` tools can build the context without
  starting the app. It reads the engine and connection string from the API's `appsettings.json`
  (`DataSource:Provider` and `ConnectionStrings:*`); environment variables are optional overrides only.

> **Note on the existing SQL scripts.** The idempotent scripts under `Migrations/Scripts/**` and the
> startup `MigrationRunner` still exist and remain valid. If you adopt EF migrations as the schema authority,
> run `dotnet ef database update` to create/upgrade the schema; the startup `MigrationRunner` is idempotent
> (`IF NOT EXISTS` guards) so it is harmless if left enabled, but you can also stop relying on it.

---

## 1. Install the tooling

The EF command-line tool is a .NET global tool. Install it once (the version tracks EF Core 10):

```bash
dotnet tool install --global dotnet-ef --version 10.0.0
# If the tool isn't found afterwards, add the global tools folder to PATH:
export PATH="$PATH:$HOME/.dotnet/tools"     # Linux/macOS
# Windows (PowerShell):  $env:PATH += ";$env:USERPROFILE\.dotnet\tools"
```

Verify:

```bash
dotnet ef --version      # should print 10.0.x
```

The `Microsoft.EntityFrameworkCore.Design` package is already referenced by `Tedwren.DataAccess`, so no
project changes are needed.

---

## 2. Choose the engine and connection string

**The design-time factory reads the engine and connection string from the API's
`src/Tedwren.Api/appsettings.json` — the same file the running app uses. No environment variables are
required.** Configure the database there:

| `appsettings.json` key | Values | Notes |
|---|---|---|
| `DataSource:Provider` | `SqlServer` (default) or `PostgreSql` | Selects the migration SQL dialect. |
| `ConnectionStrings:SqlServer` | a connection string | Used when the provider is SQL Server. |
| `ConnectionStrings:PostgreSql` | a connection string | Used when the provider is PostgreSQL. |

The factory locates `appsettings.json` by walking up to the repository root, so the `dotnet ef` commands work
from any directory. If `ASPNETCORE_ENVIRONMENT` (or `DOTNET_ENVIRONMENT`) is set, the matching
`appsettings.{Environment}.json` overlay is applied on top, exactly as the API host does.

> **Optional overrides.** The `TEDWREN_EF_PROVIDER` and `TEDWREN_EF_CONNECTION` environment variables still
> override the file when set, and standard `ConnectionStrings__SqlServer`-style environment variables are also
> honoured — but none of these are needed for the normal `appsettings.json`-driven workflow.

All `dotnet ef` commands below point at the data-access project for both the migrations project (`-p`) and
the startup project (`-s`), because the design-time factory removes the need for the API host:

```
-p src/Tedwren.DataAccess -s src/Tedwren.DataAccess
```

---

## 3. Create the initial migration

From the repository root:

```bash
dotnet ef migrations add InitialCreate \
  -p src/Tedwren.DataAccess -s src/Tedwren.DataAccess \
  -o Ef/Migrations
```

This generates the migration and a model snapshot under `src/Tedwren.DataAccess/Ef/Migrations`. It does not
touch any database. Review the generated `*_InitialCreate.cs` before applying.

---

## 4. Apply migrations to a database

Point `TEDWREN_EF_CONNECTION` at the target database, then:

```bash
dotnet ef database update \
  -p src/Tedwren.DataAccess -s src/Tedwren.DataAccess
```

This creates the tables (and the `__EFMigrationsHistory` tracking table) and brings the database up to the
latest migration. Re-running it when there are no pending migrations is a no-op.

To point the running API at that database, set the API to database mode in
`src/Tedwren.Api/appsettings.json`:

```jsonc
"DataSource": { "Mode": "Database", "Provider": "SqlServer" },
"ConnectionStrings": { "SqlServer": "…same connection string…" }
```

---

## 5. Add a later schema change

1. Edit the relevant class in `src/Tedwren.DataAccess/Ef/SchemaRecords.cs` (and the mapping in
   `TedwrenDbContext.cs` if you add a table or index) **and** the matching Dapper repository/SQL.
2. Create a migration:

   ```bash
   dotnet ef migrations add <DescriptiveName> \
     -p src/Tedwren.DataAccess -s src/Tedwren.DataAccess -o Ef/Migrations
   ```
3. Apply it:

   ```bash
   dotnet ef database update -p src/Tedwren.DataAccess -s src/Tedwren.DataAccess
   ```

Undo the last (unapplied) migration with:

```bash
dotnet ef migrations remove -p src/Tedwren.DataAccess -s src/Tedwren.DataAccess
```

---

## 6. Produce a SQL script instead of applying directly

Useful for DBA review or CI-controlled deployments (generates an idempotent script):

```bash
dotnet ef migrations script --idempotent \
  -p src/Tedwren.DataAccess -s src/Tedwren.DataAccess \
  -o schema.sql
```

---

## 7. PostgreSQL

PostgreSQL support is authored (the context folds identifiers to lower case) but its migrations and
full parity run are **deferred with the PostgreSQL launch gate**. When you pick it up: EF migrations are
provider-specific, so generate the PostgreSQL set in a clean working tree with
`TEDWREN_EF_PROVIDER=PostgreSql` and a separate output directory, keeping the two providers' migration
folders apart. SQL Server is the supported EF path today.

---

## 8. Two databases: product and commercial

The runtime uses **two databases**. The product/compliance data lives in the primary database
(`ConnectionStrings:SqlServer`); the commercial/admin plane — subscriptions, payments, mandates, payouts,
webhook events, and the go-to-market slices (launch list, and later leads and affiliates) — lives in a
**separate commercial database** (`ConnectionStrings:SqlServerCommercial`, and `PostgreSqlCommercial`). When
the commercial connection string is empty it falls back to the product connection string, so a single-database
dev setup still runs.

- **Runtime schema (authoritative):** the idempotent SQL scripts under
  `src/Tedwren.DataAccess/Migrations/Scripts/{SqlServer,Postgres}/` are split into two areas — everything at
  the engine-folder root is the **product** set, and the `Commercial/` subfolder is the **commercial** set.
  `MigrationRunner.RunAsync(factory, area)` runs each area against its own database; `Program.cs` calls it once
  per database at startup. Add a new commercial table's script under `.../Commercial/` (continue the number
  sequence, e.g. `025_*`), and a new product table's script at the engine-folder root.
- **EF migrations:** the EF `TedwrenDbContext` (in `Tedwren.DataAccess`) covers the **product** database. The
  **commercial** database has its own EF context, `CommercialDbContext`, in the separate
  **`Tedwren.DataAccess.Commercial`** project (`Ef/CommercialDbContext.cs`, `Ef/CommercialSchemaRecords.cs`,
  design-time `Ef/CommercialDbContextFactory.cs`). Its design-time factory reads
  `ConnectionStrings:SqlServerCommercial` (falling back to `ConnectionStrings:SqlServer` when empty, mirroring the
  runtime fallback); `TEDWREN_EF_COMMERCIAL_CONNECTION` overrides it. Because two contexts are now discoverable,
  pass `--context` and point `-p/-s` at the commercial project:

  ```bash
  # create/evolve the commercial migration
  dotnet ef migrations add <Name> --context CommercialDbContext \
    -p src/Tedwren.DataAccess.Commercial -s src/Tedwren.DataAccess.Commercial -o Ef/Migrations

  # apply to the commercial database
  dotnet ef database update --context CommercialDbContext \
    -p src/Tedwren.DataAccess.Commercial -s src/Tedwren.DataAccess.Commercial
  ```

  EF is authoritative for the commercial schema; the idempotent commercial scripts remain valid and the startup
  `MigrationRunner` (Commercial area) stays a no-op over EF-created tables (the EF mappings reproduce the scripts'
  table/column/index names). **PostgreSQL** commercial EF is still **deferred** with the Postgres launch gate
  (§7) — the commercial SQL scripts cover Postgres in the meantime.
- **Relocation of the billing tables:** scripts `022`–`024` (billing, webhook events, payouts) moved from the
  product set into the commercial set. Fresh environments get these created directly in the commercial
  database. **An already-populated product database** that predates this change still has those tables in the
  product database; migrate them with a one-off data copy into the commercial database (e.g. `SELECT INTO` /
  `INSERT … SELECT` across the two catalogues, or a scripted export/import), then drop the orphaned product
  copies once verified. There is no automatic data move — the scripts only create empty tables.

---

## 9. Rebuild both databases from scratch

A clean-slate rebuild: drop **both** databases and recreate a single fresh `InitialCreate` migration per
context. Use this when you want to squash a tangle of migrations back to one, or reset a dev/staging
environment. There are two EF contexts — `TedwrenDbContext` (product, in `Tedwren.DataAccess`) and
`CommercialDbContext` (commercial, in `Tedwren.DataAccess.Commercial`) — so every command names its `--context`
and points `-p`/`-s` at the right project.

> ⚠️ **DESTRUCTIVE.** `dotnet ef database drop` deletes the **entire database and all its data** (companies,
> persons, compliance, users, billing — everything), not just the schema, and is **not reversible** without a
> backup. Only run this against databases you intend to wipe. **Take a backup first.**
>
> ⚠️ **Commercial fallback trap.** If `ConnectionStrings:SqlServerCommercial` is empty, `CommercialDbContext`
> falls back to the **product** connection string — so the commercial `database drop` would hit the *same*
> database as the product one. Always run Step 0 first and confirm the two contexts resolve to **two distinct
> databases** before dropping.

**Prerequisite:** delete the existing migration files for both projects first (including each
`*ModelSnapshot.cs`) — `src/Tedwren.DataAccess/Ef/Migrations/*` and
`src/Tedwren.DataAccess.Commercial/Ef/Migrations/*`. Both projects still build with the folders empty.

**Step 0 — verify the two targets (no changes made):**

```bash
dotnet ef dbcontext info --context TedwrenDbContext \
  -p src/Tedwren.DataAccess -s src/Tedwren.DataAccess

dotnet ef dbcontext info --context CommercialDbContext \
  -p src/Tedwren.DataAccess.Commercial -s src/Tedwren.DataAccess.Commercial
```

Stop unless the two `Data source` / `Database name` lines are the distinct databases you mean to drop.

**Step 1 — drop both databases** (`-f` skips the interactive confirmation):

```bash
dotnet ef database drop -f --context TedwrenDbContext \
  -p src/Tedwren.DataAccess -s src/Tedwren.DataAccess

dotnet ef database drop -f --context CommercialDbContext \
  -p src/Tedwren.DataAccess.Commercial -s src/Tedwren.DataAccess.Commercial
```

**Step 2 — create a fresh initial migration for each context:**

```bash
dotnet ef migrations add InitialCreate --context TedwrenDbContext \
  -p src/Tedwren.DataAccess -s src/Tedwren.DataAccess -o Ef/Migrations

dotnet ef migrations add InitialCreate --context CommercialDbContext \
  -p src/Tedwren.DataAccess.Commercial -s src/Tedwren.DataAccess.Commercial -o Ef/Migrations
```

**Step 3 — recreate the schema by applying each migration:**

```bash
dotnet ef database update --context TedwrenDbContext \
  -p src/Tedwren.DataAccess -s src/Tedwren.DataAccess

dotnet ef database update --context CommercialDbContext \
  -p src/Tedwren.DataAccess.Commercial -s src/Tedwren.DataAccess.Commercial
```

Each squashed `InitialCreate` captures the **current full model** (all product tables incl. the later Forms /
CompanyDocuments slices; all 11 commercial tables), so the rebuilt schema is complete. The idempotent startup
`MigrationRunner` scripts still run and stay a no-op over the EF-created tables (matching table/column/index
names). Naming both migrations `InitialCreate` is fine — they live in separate assemblies.

---

## Quick reference

```bash
# one-time
dotnet tool install --global dotnet-ef --version 10.0.0
export PATH="$PATH:$HOME/.dotnet/tools"

# per database
export TEDWREN_EF_PROVIDER=SqlServer
export TEDWREN_EF_CONNECTION="Server=(localdb)\\MSSQLLocalDB;Database=Tedwren;Trusted_Connection=True;TrustServerCertificate=True"

# first time only
dotnet ef migrations add InitialCreate -p src/Tedwren.DataAccess -s src/Tedwren.DataAccess -o Ef/Migrations

# create/upgrade the database
dotnet ef database update -p src/Tedwren.DataAccess -s src/Tedwren.DataAccess
```
