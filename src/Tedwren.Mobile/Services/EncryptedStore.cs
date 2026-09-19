using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Tedwren.Mobile.Core.Caching;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.Sync;

namespace Tedwren.Mobile.Services;

/// <summary>
/// The encrypted on-device store (M5), backing both the read cache (<see cref="IReadCache"/>) and the append-only
/// sync outbox (<see cref="IOutboxStore"/>) in one SQLCipher (AES-256) database. The database key is a 256-bit random
/// value held in the OS secure enclave via <see cref="ISecureStore"/> (Keychain / Keystore). Initialisation is lazy
/// and single-flighted (the key fetch is async), so app start is not blocked. Read-cache reads/writes fail soft (a
/// storage error must never blank a screen); outbox operations surface errors to the sync engine.
/// </summary>
public sealed class EncryptedStore : IReadCache, IOutboxStore
{
    /// <summary>Secure-store key under which the database encryption key is held.</summary>
    private const string DbKeyName = "tedwren.db.key";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ISecureStore _secure;
    private readonly string _dbPath;
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private string? _connectionString;

    static EncryptedStore() => SQLitePCL.Batteries_V2.Init();

    /// <summary>Creates the store over the secure key store and the database file path.</summary>
    public EncryptedStore(ISecureStore secure, string dbPath)
    {
        _secure = secure;
        _dbPath = dbPath;
    }

    // ---- IReadCache --------------------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Json FROM ReadCache WHERE Key = $key";
            command.Parameters.AddWithValue("$key", key);
            var json = (string?)await command.ExecuteScalarAsync(cancellationToken);
            return json is null ? default : JsonSerializer.Deserialize<T>(json, Json);
        }
        catch
        {
            return default; // fail soft — a cache miss/error must not blank the screen.
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText =
                "INSERT INTO ReadCache (Key, Json, UpdatedUtc) VALUES ($key, $json, $utc) " +
                "ON CONFLICT(Key) DO UPDATE SET Json = $json, UpdatedUtc = $utc";
            command.Parameters.AddWithValue("$key", key);
            command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(value, Json));
            command.Parameters.AddWithValue("$utc", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            // fail soft — losing a cache write is acceptable; it is repopulated from the network next time.
        }
    }

    // ---- IOutboxStore ------------------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<OutboxItem> EnqueueAsync(OutboxItem item, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);

        await using (var seq = connection.CreateCommand())
        {
            seq.CommandText = "SELECT COALESCE(MAX(Sequence), 0) + 1 FROM Outbox";
            item.Sequence = Convert.ToInt64(await seq.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO Outbox (Id, Sequence, Kind, PayloadJson, PhotoBytes, PhotoContentType, UploadedImageReference, " +
            "Status, AttemptCount, NextAttemptUtc, LastError, CreatedUtc, CompletedUtc) VALUES " +
            "($id, $seq, $kind, $payload, $photo, $photoType, $imageRef, $status, $attempts, $next, $error, $created, $completed)";
        BindItem(command, item);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return item;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxItem>> GetDrainableAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT {Columns} FROM Outbox WHERE Status IN ($pending, $failed) " +
            "AND (NextAttemptUtc IS NULL OR NextAttemptUtc <= $now) ORDER BY Sequence";
        command.Parameters.AddWithValue("$pending", (int)OutboxItemStatus.Pending);
        command.Parameters.AddWithValue("$failed", (int)OutboxItemStatus.Failed);
        command.Parameters.AddWithValue("$now", now.ToString("O", CultureInfo.InvariantCulture));

        var items = new List<OutboxItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ReadItem(reader));
        }

        return items;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(OutboxItem item, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE Outbox SET Kind = $kind, PayloadJson = $payload, PhotoBytes = $photo, PhotoContentType = $photoType, " +
            "UploadedImageReference = $imageRef, Status = $status, AttemptCount = $attempts, NextAttemptUtc = $next, " +
            "LastError = $error, CreatedUtc = $created, CompletedUtc = $completed WHERE Id = $id";
        BindItem(command, item);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OutboxCounts> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT " +
            "SUM(CASE WHEN Status IN ($pending, $failed) THEN 1 ELSE 0 END), " +
            "SUM(CASE WHEN Status = $attention THEN 1 ELSE 0 END) FROM Outbox";
        command.Parameters.AddWithValue("$pending", (int)OutboxItemStatus.Pending);
        command.Parameters.AddWithValue("$failed", (int)OutboxItemStatus.Failed);
        command.Parameters.AddWithValue("$attention", (int)OutboxItemStatus.NeedsAttention);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var pending = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            var attention = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            return new OutboxCounts(pending, attention);
        }

        return new OutboxCounts(0, 0);
    }

    // ---- infrastructure ----------------------------------------------------------------------------------

    private const string Columns =
        "Id, Sequence, Kind, PayloadJson, PhotoBytes, PhotoContentType, UploadedImageReference, Status, " +
        "AttemptCount, NextAttemptUtc, LastError, CreatedUtc, CompletedUtc";

    private const string Schema =
        "CREATE TABLE IF NOT EXISTS ReadCache (Key TEXT PRIMARY KEY, Json TEXT NOT NULL, UpdatedUtc TEXT NOT NULL);" +
        "CREATE TABLE IF NOT EXISTS Outbox (" +
        "Id TEXT PRIMARY KEY, Sequence INTEGER NOT NULL, Kind TEXT NOT NULL, PayloadJson TEXT NOT NULL, " +
        "PhotoBytes BLOB NULL, PhotoContentType TEXT NULL, UploadedImageReference TEXT NULL, Status INTEGER NOT NULL, " +
        "AttemptCount INTEGER NOT NULL, NextAttemptUtc TEXT NULL, LastError TEXT NULL, CreatedUtc TEXT NOT NULL, " +
        "CompletedUtc TEXT NULL);";

    /// <summary>Opens a fresh connection, ensuring the key + schema exist (single-flighted).</summary>
    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(await ConnectionStringAsync(cancellationToken));
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private async Task<string> ConnectionStringAsync(CancellationToken cancellationToken)
    {
        if (_connectionString is not null)
        {
            return _connectionString;
        }

        await _initGate.WaitAsync(cancellationToken);
        try
        {
            if (_connectionString is not null)
            {
                return _connectionString;
            }

            var key = await _secure.GetAsync(DbKeyName);
            if (string.IsNullOrEmpty(key))
            {
                key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                await _secure.SetAsync(DbKeyName, key);
            }

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Password = key, // SQLCipher: issues PRAGMA key, encrypting the database at rest.
            }.ToString();

            await using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                await using var schema = connection.CreateCommand();
                schema.CommandText = Schema;
                await schema.ExecuteNonQueryAsync(cancellationToken);
            }

            _connectionString = connectionString;
            return _connectionString;
        }
        finally
        {
            _initGate.Release();
        }
    }

    /// <summary>Binds every outbox column parameter from an item (shared by insert + update).</summary>
    private static void BindItem(SqliteCommand command, OutboxItem item)
    {
        command.Parameters.AddWithValue("$id", item.Id.ToString());
        command.Parameters.AddWithValue("$seq", item.Sequence);
        command.Parameters.AddWithValue("$kind", item.Kind);
        command.Parameters.AddWithValue("$payload", item.PayloadJson);
        command.Parameters.AddWithValue("$photo", (object?)item.PhotoBytes ?? DBNull.Value);
        command.Parameters.AddWithValue("$photoType", (object?)item.PhotoContentType ?? DBNull.Value);
        command.Parameters.AddWithValue("$imageRef", (object?)item.UploadedImageReference ?? DBNull.Value);
        command.Parameters.AddWithValue("$status", (int)item.Status);
        command.Parameters.AddWithValue("$attempts", item.AttemptCount);
        command.Parameters.AddWithValue("$next", (object?)item.NextAttemptUtc?.ToString("O", CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$error", (object?)item.LastError ?? DBNull.Value);
        command.Parameters.AddWithValue("$created", item.CreatedUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$completed", (object?)item.CompletedUtc?.ToString("O", CultureInfo.InvariantCulture) ?? DBNull.Value);
    }

    /// <summary>Reads an outbox row (column order matches <see cref="Columns"/>).</summary>
    private static OutboxItem ReadItem(SqliteDataReader reader) => new()
    {
        Id = Guid.Parse(reader.GetString(0)),
        Sequence = reader.GetInt64(1),
        Kind = reader.GetString(2),
        PayloadJson = reader.GetString(3),
        PhotoBytes = reader.IsDBNull(4) ? null : (byte[])reader.GetValue(4),
        PhotoContentType = reader.IsDBNull(5) ? null : reader.GetString(5),
        UploadedImageReference = reader.IsDBNull(6) ? null : reader.GetString(6),
        Status = (OutboxItemStatus)reader.GetInt32(7),
        AttemptCount = reader.GetInt32(8),
        NextAttemptUtc = reader.IsDBNull(9) ? null : ParseUtc(reader.GetString(9)),
        LastError = reader.IsDBNull(10) ? null : reader.GetString(10),
        CreatedUtc = ParseUtc(reader.GetString(11)),
        CompletedUtc = reader.IsDBNull(12) ? null : ParseUtc(reader.GetString(12)),
    };

    private static DateTimeOffset ParseUtc(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
