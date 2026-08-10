# EF Core database migrations

This project uses **EF Core to author and apply the database schema (DDL) via migrations**, while runtime
data access stays on **Dapper (DML)**. The two are deliberately separate:

| Concern | Owner |
|---|---|
| Creating / evolving tables, columns, indexes | EF Core migrations (`Tedwren.DataAccess/Ef`) |
| Reading / writing rows at runtime | Dapper repositories (`Tedwren.DataAccess/Repositories`) |

The EF model lives in `src/Tedwren.DataAccess/Ef`:

- `SchemaRecords.cs` — flat classes that mirror each table 1:1. They exist only to describe the schema and
  are never used at runtime.
- `TedwrenDbContext.cs` — table/column/index mappings. On PostgreSQL every identifier is folded to lower
  case so the Dapper repositories' unquoted SQL resolves to the EF-created tables.
- `TedwrenDbContextFactory.cs` — a design-time factory so the `dotnet ef` tools can build the context without
  starting the app. It reads the engine and connection string from environment variables.

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

Both are supplied via environment variables read by the design-time factory:

| Variable | Values | Notes |
|---|---|---|
| `TEDWREN_EF_PROVIDER` | `SqlServer` (default) or `PostgreSql` | Selects the migration SQL dialect. |
| `TEDWREN_EF_CONNECTION` | a connection string | Only needed for `database update` / `script` against a live DB. `migrations add` does **not** connect. |

Examples:

```bash
# SQL Server (LocalDB)
export TEDWREN_EF_PROVIDER=SqlServer
export TEDWREN_EF_CONNECTION="Server=(localdb)\\MSSQLLocalDB;Database=Tedwren;Trusted_Connection=True;TrustServerCertificate=True"

# PostgreSQL
export TEDWREN_EF_PROVIDER=PostgreSql
export TEDWREN_EF_CONNECTION="Host=localhost;Port=5432;Database=tedwren;Username=postgres;Password=postgres"
```

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
