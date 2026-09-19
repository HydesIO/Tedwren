# Object storage for private assets (LR-4)

Private binary assets — card photographs (SF-5) and uploaded documents (RAMS/insurance/HSE files) — are held
behind the `IImageStore` interface. They are **never** exposed as public URLs; they are served only through the
authorised image endpoint (R9).

Two backing stores are supported, chosen by configuration:

| `Storage:Provider` | Backing store | When to use |
|---|---|---|
| `Database` (default) | BLOBs in the product database (`StoredImages`) | Local dev, tests, a single-box deployment |
| `S3` | An S3-compatible object store | Production — offloads large binaries from the database |

The default is `Database`, so **nothing changes until S3 is configured** — no external dependency is required to
run the app or the test suite.

## Using iDrive e2 (or any S3-compatible store)

iDrive e2 is S3-compatible: it exposes an S3 API at a per-bucket endpoint and uses path-style addressing. Set the
`Storage` section (values below are secrets — set them in the environment or a secret store, **never** in source):

```jsonc
"Storage": {
  "Provider": "S3",
  "S3": {
    "ServiceUrl": "https://<your-bucket-endpoint>.idrivee2-XX.com",  // from the iDrive e2 console
    "Region": "us-east-1",        // a placeholder region is fine; used only for request signing
    "Bucket": "<your-bucket>",
    "AccessKey": "<access-key>",
    "SecretKey": "<secret-key>",
    "ForcePathStyle": true,        // required by iDrive e2 (and most S3-compatible providers)
    "KeyPrefix": "tedwren/"        // optional namespace for the objects
  }
}
```

Configuration binds from environment variables too (nested keys use `__`):

```bash
export Storage__Provider=S3
export Storage__S3__ServiceUrl="https://<your-bucket-endpoint>.idrivee2-XX.com"
export Storage__S3__Region=us-east-1
export Storage__S3__Bucket=<your-bucket>
export Storage__S3__AccessKey=<access-key>
export Storage__S3__SecretKey=<secret-key>
export Storage__S3__ForcePathStyle=true
export Storage__S3__KeyPrefix=tedwren/
```

### Notes

- **Fail-fast.** If `Storage:Provider` is `S3` but `Bucket`, `AccessKey` or `SecretKey` is missing, the API
  refuses to start with a message naming the missing setting — it will not silently fall back to the database.
- **Keys.** Each asset is stored under `<KeyPrefix><GUID>`; the reference handed back to the rest of the app is the
  GUID string, interchangeable with the database store's references. Only a GUID reference is accepted on read, so a
  reference cannot address an arbitrary object key.
- **Bucket privacy.** Keep the bucket private. Tedwren streams bytes back through its own authorised endpoint; it
  never issues public object URLs (R9).
- **Verification.** A live S3 round-trip is a deployment-time check (like the SQL Server LocalDB integration suite):
  after setting the config, upload a card photo and confirm it is retrievable in the app.
