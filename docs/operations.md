# Operations runbook

Deployment/operational steps for the commercial (admin) plane. See `docs/ef-migrations.md §8` for the
database-topology background.

## 1. Required secrets & the production startup guard

The API is **secure-by-default and fails closed**: in the `Production` environment it refuses to start while any
committed development default is still in effect. Supply these from the environment or a secret store (never
commit them). Environment variables use the standard ASP.NET `Section__Key` form.

| Setting | Env var | Why it's required in Production |
|---|---|---|
| `Jwt:SigningKey` | `Jwt__SigningKey` | Signs/validates console JWTs (HMAC-SHA256). Must be a unique secret of at least 32 bytes; the committed dev key is rejected, so a leaked source tree cannot forge Administrator tokens. |
| `Seed:Password` | `Seed__Password` | The bootstrap/master-admin seed password. Must be a strong non-default value; each seeded admin should change it on first sign-in. |
| `ConnectionStrings:SqlServer` | `ConnectionStrings__SqlServer` | Product database. No credential is committed — supply it here. |
| `ConnectionStrings:SqlServerCommercial` | `ConnectionStrings__SqlServerCommercial` | Commercial plane DB (see §2). Falls back to the product DB when unset. |

`Auth:TestBypass` must be **unset/false** in Production — it authenticates every request as an Administrator and
exists for the test host only; startup refuses to boot if it is true.

If any required value is missing or still a dev default, the API throws at startup with a message naming each
problem (e.g. *"Refusing to start: insecure production configuration. Jwt:SigningKey is unset or the committed
development default…"*). This is intentional — fix the configuration rather than bypass the check.

> **Rotate the previously-committed credential.** Earlier revisions committed a live SQL Server credential in
> `src/Tedwren.Api/appsettings.json`. It has been removed from source, but because it remains in git history it
> must be **rotated on the database server** and supplied only via the environment/secret store from now on.

**Local development / running without a database.** The committed `appsettings.json` now ships empty connection
strings, so `dotnet run --project src/Tedwren.Api` needs either a connection string (via `dotnet user-secrets`
or `ConnectionStrings__SqlServer`) or the database-free test double — set `DataSource__Mode=InMemory` for a local
run against the in-memory repositories. (The production default remains `Database`.)

## 2. Provisioning the commercial database

The commercial/admin plane (subscriptions, payments, mandates, payouts, webhook events, launch list, leads,
affiliates) lives in a **separate database** from the product/compliance data.

- Create the catalogue on the same server as the product database (cross-database billing relocation, below,
  needs them co-located): e.g. `CREATE DATABASE TedwrenCommercial;` (SQL Server) or
  `CREATE DATABASE tedwren_commercial;` (PostgreSQL).
- Set the connection string in `src/Tedwren.Api/appsettings.json` (or the environment):
  `ConnectionStrings:SqlServerCommercial` (or `PostgreSqlCommercial`).
- The API creates the tables on startup (the area-aware `MigrationRunner` runs the `Commercial/` scripts against
  this connection). No manual DDL required.

**When the connection string is empty**, the commercial plane **falls back to the product database** — a
single-database dev setup that still runs. The API logs which topology is active at startup:

```
Commercial/admin database is SEPARATE from the product database. ...
Commercial/admin database is SHARED with (fallback) the product database. ...
```

If you expect separation and see `SHARED`, the `*Commercial` connection string is missing.

## 3. Relocating existing billing data (one-off)

Only needed for an environment whose **product** database already held billing data before the split. Fresh
environments get empty commercial tables directly and need nothing here.

- **SQL Server:** run `docs/migrations/relocate-billing-to-commercial.sql` (idempotent; replace the
  `:ProductDb` / `:CommercialDb` placeholders). Verify row counts, then drop the orphaned product-side tables.
- **PostgreSQL:** cross-database SQL isn't available, so dump the five tables from the product database and
  restore them into the commercial one:

  ```bash
  pg_dump -t mandates -t payments -t billingsubscriptions -t webhookevents -t payouts \
          --data-only --no-owner "<product-conn>" > billing.sql
  psql "<commercial-conn>" -f billing.sql
  # verify counts, then on the product DB: DROP TABLE payouts, webhookevents, payments, billingsubscriptions, mandates;
  ```

Take a backup first and run inside a transaction.

## 4. Enabling outbound email (Resend)

Launch-list and affiliate emails do **not** send until a real provider is configured — the default is the
no-op outbox (`Email:Provider = "Outbox"`). To dispatch for real, set in `appsettings.json` (or environment):

```json
"Email": {
  "Provider": "Resend",
  "ApiKey": "<resend-api-key>",
  "FromEmail": "notifications@tedwren.co.uk",
  "FromName": "Tedwren",
  "PublicBaseUrl": "https://<api-origin>",     // serves email assets + the launch unsubscribe link
  "ConsoleBaseUrl": "https://<console-origin>" // used in the affiliate agreement signing link
}
```

The API registers the real Resend HTTP sender only when `Provider = Resend` and `ApiKey` is non-empty;
otherwise the outbox stands and nothing is dispatched. `PublicBaseUrl` must be the API's public origin (it
backs `{PublicBaseUrl}/api/launch-signups/unsubscribe` and `{PublicBaseUrl}/api/email-assets/logo.png`).

## 5. Enabling outbound SMS (Twilio)

Worker SMS (SF-9 expiry warnings; the onboarding link that is the natural route to a worker's phone) does **not**
send until a provider is configured — the default is the no-op outbox (`Sms:Provider = "Outbox"`). To dispatch
for real, set in the environment (secrets — do not commit):

```json
"Sms": {
  "Provider": "Twilio",
  "AccountSid": "<twilio-account-sid>",
  "AuthToken": "<twilio-auth-token>",
  "FromNumber": "+44..."             // a Twilio number in E.164, or a messaging-service SID
}
```

The API registers the real Twilio HTTP sender only when `Provider = Twilio` and the SID, token and from-number
are all set; otherwise the outbox stands and nothing is dispatched. The sender is pluggable behind
`ISmsSender` — a different provider (e.g. Vonage) is a small addition following the same pattern. Until this is
configured, worker expiry warnings are recorded to the outbox but not delivered, so configure it before relying
on SF-9 SMS in production.

## 6. R12 job heartbeat & ops alerts

The scheduled compliance jobs (expiry scan, weekly digest, recurring-form reminders, overnight still-signed-in
check) each record a run, and a **heartbeat watchdog** (`JobHeartbeatHostedService`) checks — independently of
the job-execution loop, on its own `Jobs:HeartbeatIntervalHours` cadence (default 6h) — that each has succeeded
within its interval, emailing an alert and logging a warning if one has silently stopped (R12). Set
`Jobs:OpsEmail` to the address those alerts go to (defaults to a non-deliverable `ops@tedwren.local`), and
ensure email is configured (§4) so the alert actually leaves the building. `Jobs:SchedulerEnabled=false` turns
off both the jobs and the watchdog (used by the test host).
