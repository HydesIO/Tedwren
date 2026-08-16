# Operations runbook

Deployment/operational steps for the commercial (admin) plane. See `docs/ef-migrations.md §8` for the
database-topology background.

## 1. Provisioning the commercial database

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

## 2. Relocating existing billing data (one-off)

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

## 3. Enabling outbound email (Resend)

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
