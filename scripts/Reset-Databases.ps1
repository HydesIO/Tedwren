<#
.SYNOPSIS
    Clean-slate rebuild of BOTH Tedwren databases (product + commercial).

.DESCRIPTION
    Squashes the EF Core migration history back to a single fresh InitialCreate per
    context and recreates both databases from scratch. For each of the two EF contexts
    — TedwrenDbContext (product, in Tedwren.DataAccess) and CommercialDbContext
    (commercial, in Tedwren.DataAccess.Commercial) — the script:

        1. Drops the database                (dotnet ef database drop -f)
        2. Deletes every migration + snapshot (Ef/Migrations/*)
        3. Creates a new InitialCreate        (dotnet ef migrations add InitialCreate)
        4. Applies it                         (dotnet ef database update)

    This mirrors "Section 9 — Rebuild both databases from scratch" in
    docs/ef-migrations.md.

    The engine and connection strings are resolved by each context's design-time
    factory from src/Tedwren.Api/appsettings.json (DataSource:Provider and
    ConnectionStrings:*), so no environment variables are required.

.PARAMETER RepoRoot
    Repository root (the folder that contains Tedwren.sln). Defaults to the parent of
    the folder this script lives in.

.PARAMETER Force
    Skip the interactive "type YES to continue" confirmation. Intended for
    non-interactive/CI use — be certain you are pointed at throwaway databases.

.NOTES
    ⚠️ DESTRUCTIVE AND NOT REVERSIBLE. `dotnet ef database drop` deletes the entire
    database and ALL its data (companies, persons, compliance, users, billing —
    everything), not just the schema. Take a backup first.

    ⚠️ Commercial fallback trap: when ConnectionStrings:SqlServerCommercial is empty,
    CommercialDbContext falls back to the product connection string — so both drops
    would hit the SAME database. The script prints each context's resolved target and
    refuses to continue if the two contexts resolve to the same database (override with
    -Force if that is genuinely intended).

.EXAMPLE
    ./scripts/Reset-Databases.ps1

.EXAMPLE
    ./scripts/Reset-Databases.ps1 -RepoRoot 'D:\Projects\HeyiO\HeyIO.Tedwren' -Force
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

# --- Context definitions --------------------------------------------------------------
# Each EF context, its owning project (used for both -p migrations and -s startup), and
# the migrations output directory relative to that project.
$Contexts = @(
    [pscustomobject]@{
        Name    = 'TedwrenDbContext'
        Project = 'src/Tedwren.DataAccess'
        Output  = 'Ef/Migrations'
    },
    [pscustomobject]@{
        Name    = 'CommercialDbContext'
        Project = 'src/Tedwren.DataAccess.Commercial'
        Output  = 'Ef/Migrations'
    }
)

# --- Helpers --------------------------------------------------------------------------

# Runs a dotnet ef command for a context and throws on a non-zero exit code so the
# script stops rather than pressing on after a failed step.
function Invoke-Ef {
    param(
        [Parameter(Mandatory)][string[]]$EfArgs,
        [Parameter(Mandatory)][pscustomobject]$Context
    )

    $full = @('ef') + $EfArgs + @(
        '--context', $Context.Name,
        '-p', $Context.Project,
        '-s', $Context.Project
    )

    Write-Host "  > dotnet $($full -join ' ')" -ForegroundColor DarkGray
    & dotnet @full
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef failed (exit $LASTEXITCODE) for context '$($Context.Name)'."
    }
}

# Returns the "provider:database" target string a context resolves to, captured from
# `dotnet ef dbcontext info`. Used to prove the two contexts hit two distinct databases.
function Get-ContextTarget {
    param([Parameter(Mandatory)][pscustomobject]$Context)

    $info = & dotnet ef dbcontext info `
        --context $Context.Name `
        -p $Context.Project -s $Context.Project 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host ($info -join [Environment]::NewLine) -ForegroundColor Red
        throw "Could not read dbcontext info for '$($Context.Name)'. Check the connection string in src/Tedwren.Api/appsettings.json."
    }

    $provider = ($info | Select-String -Pattern '^\s*Provider name:\s*(.+)$').Matches.Groups[1].Value
    $database = ($info | Select-String -Pattern '^\s*Database name:\s*(.+)$').Matches.Groups[1].Value
    $source   = ($info | Select-String -Pattern '^\s*Data source:\s*(.+)$').Matches.Groups[1].Value

    return [pscustomobject]@{
        Context  = $Context.Name
        Provider = ($provider ?? '(unknown)').Trim()
        Source   = ($source   ?? '(unknown)').Trim()
        Database = ($database ?? '(unknown)').Trim()
        Key      = "$source/$database".Trim()
    }
}

# --- Preconditions --------------------------------------------------------------------

Set-Location $RepoRoot

if (-not (Test-Path (Join-Path $RepoRoot 'Tedwren.sln'))) {
    throw "Tedwren.sln not found under '$RepoRoot'. Pass -RepoRoot pointing at the repository root."
}

# dotnet must be on PATH.
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "The 'dotnet' CLI was not found on PATH. Install the .NET 10 SDK first."
}

# The dotnet-ef global tool must be installed (see docs/ef-migrations.md §1).
& dotnet ef --version *> $null
if ($LASTEXITCODE -ne 0) {
    throw "The 'dotnet ef' tool is not available. Install it with: dotnet tool install --global dotnet-ef --version 10.0.0"
}

Write-Host "Repository root : $RepoRoot" -ForegroundColor Cyan
Write-Host ''

# --- Step 0: resolve and verify the two targets (no changes made) ---------------------

Write-Host 'Resolving database targets from src/Tedwren.Api/appsettings.json ...' -ForegroundColor Cyan
$targets = foreach ($ctx in $Contexts) { Get-ContextTarget -Context $ctx }
$targets | Format-Table Context, Provider, Source, Database -AutoSize | Out-String | Write-Host

$distinctTargets = ($targets | Select-Object -ExpandProperty Key | Sort-Object -Unique)
if ($distinctTargets.Count -lt $Contexts.Count) {
    Write-Host 'WARNING: the two contexts resolve to the SAME database.' -ForegroundColor Yellow
    Write-Host 'This usually means ConnectionStrings:SqlServerCommercial is empty and the' -ForegroundColor Yellow
    Write-Host 'commercial context has fallen back to the product connection string.' -ForegroundColor Yellow
    if (-not $Force) {
        throw "Refusing to drop: both contexts point at the same database. Set a distinct ConnectionStrings:SqlServerCommercial, or pass -Force if this is intentional."
    }
    Write-Host '-Force supplied: continuing against the shared database.' -ForegroundColor Yellow
    Write-Host ''
}

# --- Confirmation ---------------------------------------------------------------------

if (-not $Force) {
    Write-Host 'This will PERMANENTLY DROP the databases listed above and delete all their data.' -ForegroundColor Red
    Write-Host 'This action cannot be undone. Take a backup first.' -ForegroundColor Red
    $answer = Read-Host 'Type YES (all caps) to continue'
    if ($answer -ne 'YES') {
        Write-Host 'Aborted. No changes made.' -ForegroundColor Yellow
        return
    }
}

# --- Rebuild each context -------------------------------------------------------------

foreach ($ctx in $Contexts) {
    Write-Host ''
    Write-Host "=== $($ctx.Name) ($($ctx.Project)) ===" -ForegroundColor Green

    # Step 1: drop the database (-f skips EF's own interactive confirmation).
    Write-Host '[1/4] Dropping database ...' -ForegroundColor Cyan
    Invoke-Ef -Context $ctx -EfArgs @('database', 'drop', '-f')

    # Step 2: delete existing migrations + model snapshot so the next add starts clean.
    Write-Host '[2/4] Deleting existing migrations and snapshot ...' -ForegroundColor Cyan
    $migrationsDir = Join-Path $RepoRoot (Join-Path $ctx.Project $ctx.Output)
    if (Test-Path $migrationsDir) {
        Get-ChildItem -Path $migrationsDir -Filter '*.cs' -File |
            ForEach-Object {
                Write-Host "      removing $($_.Name)" -ForegroundColor DarkGray
                Remove-Item -LiteralPath $_.FullName -Force
            }
    }
    else {
        Write-Host "      (no migrations directory at $migrationsDir — nothing to delete)" -ForegroundColor DarkGray
    }

    # Step 3: create a fresh InitialCreate that captures the current full model.
    Write-Host '[3/4] Creating new InitialCreate migration ...' -ForegroundColor Cyan
    Invoke-Ef -Context $ctx -EfArgs @('migrations', 'add', 'InitialCreate', '-o', $ctx.Output)

    # Step 4: apply the migration to (re)create the schema.
    Write-Host '[4/4] Applying migration (database update) ...' -ForegroundColor Cyan
    Invoke-Ef -Context $ctx -EfArgs @('database', 'update')

    Write-Host "$($ctx.Name): rebuilt." -ForegroundColor Green
}

Write-Host ''
Write-Host 'Done. Both databases have been dropped, migrations reset to a single InitialCreate, and the schema recreated.' -ForegroundColor Green
