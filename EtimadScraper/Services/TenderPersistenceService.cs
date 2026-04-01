using EtimadScraper.Data;
using EtimadScraper.Entities;
using EtimadScraper.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace EtimadScraper.Services;

/// <summary>
/// Handles persistence of scraped tenders with upsert (insert-or-update) semantics.
/// Registered as Scoped — one instance per DI scope / job execution.
/// </summary>
public class TenderPersistenceService
{
    private readonly TenderDbContext _db;
    private readonly ILogger<TenderPersistenceService> _logger;

    // Column max-lengths mirror TenderDbContext configuration exactly.
    // Keeping them in one place makes it impossible for the two to diverge.
    private static class MaxLen
    {
        public const int TenderNumber  = 500;
        public const int Title         = 2000;
        public const int Organization  = 1000;
        public const int PublishDate   = 500;
        public const int ClosingDate   = 500;
        public const int DetailsUrl    = 2000;
        public const int Category      = 1000;
        public const int Department    = 1000;
        public const int Status        = 500;
        public const int AdditionalInfo = 4000;
    }

    public TenderPersistenceService(TenderDbContext db, ILogger<TenderPersistenceService> logger)
    {
        _db     = db     ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>
    /// Upserts a list of tenders (insert new / update existing) based on
    /// <c>TenderNumber</c> as the business key, falling back to <c>DetailsUrl</c>.
    ///
    /// Algorithm:
    ///   1. Validate and resolve business key for each DTO.
    ///   2. One SELECT to load all already-existing rows for this batch.
    ///   3. In-memory split: INSERTs for new rows, property updates for existing.
    ///   4. One <see cref="DbContext.SaveChangesAsync"/> for the whole batch.
    ///   5. Verification SELECT to confirm the expected count landed in SQL Server.
    /// </summary>
    public async Task<(int Inserted, int Updated, int Failed)> UpsertBatchAsync(
        IReadOnlyList<TenderDto> dtos,
        CancellationToken cancellationToken = default)
    {
        int inserted = 0, updated = 0, failed = 0;

        if (dtos.Count == 0)
        {
            _logger.LogDebug("UpsertBatchAsync called with empty list — nothing to do.");
            return (0, 0, 0);
        }

        _logger.LogInformation("UpsertBatchAsync: starting upsert of {Count} DTOs.", dtos.Count);

        // ?? Step 1: validate + resolve business keys ?????????????????????????
        var keyed = new List<(string Key, TenderDto Dto)>(dtos.Count);

        foreach (var dto in dtos)
        {
            var validationError = ValidateDto(dto);
            if (validationError is not null)
            {
                _logger.LogWarning("Skipping invalid tender [{Title}]: {Reason}", dto.Title, validationError);
                failed++;
                continue;
            }

            var key = ResolveBusinessKey(dto);
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning(
                    "Tender has no usable business key (TenderNumber and DetailsUrl are both empty). " +
                    "Skipping: [{Title}]", dto.Title);
                failed++;
                continue;
            }

            keyed.Add((key, dto));
        }

        // De-duplicate within the incoming batch by business key.
        // Keep the last occurrence so newer page data wins.
        if (keyed.Count > 1)
        {
            keyed = keyed
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Last())
                .ToList();
        }

        if (keyed.Count == 0)
        {
            _logger.LogWarning("UpsertBatchAsync: all {Count} DTOs were invalid — nothing saved.", dtos.Count);
            return (0, 0, failed);
        }

        _logger.LogDebug("UpsertBatchAsync: {Valid} valid DTOs after validation/de-dup ({Failed} skipped).",
            keyed.Count, failed);

        // ?? Step 2: single SELECT — load existing rows for the whole batch ????
        var batchKeys = keyed.Select(k => k.Key).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        _logger.LogDebug("UpsertBatchAsync: querying DB for {Count} distinct keys...", batchKeys.Count);

        Dictionary<string, TenderEntity> existingEntities;
        try
        {
            existingEntities = await _db.Tenders
                .Where(t => batchKeys.Contains(t.TenderNumber))
                .ToDictionaryAsync(t => t.TenderNumber, StringComparer.OrdinalIgnoreCase, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpsertBatchAsync: failed to query existing tenders from DB. Aborting batch.");
            return (0, 0, dtos.Count);
        }

        _logger.LogDebug("UpsertBatchAsync: found {Existing} existing rows in DB out of {Total} keys.",
            existingEntities.Count, batchKeys.Count);

        // ?? Step 3: classify — INSERT new / UPDATE existing ???????????????????
        foreach (var (key, dto) in keyed)
        {
            try
            {
                if (existingEntities.TryGetValue(key, out var existing))
                {
                    ApplyUpdate(existing, dto);
                    updated++;
                    _logger.LogDebug("UpsertBatchAsync: marked [{Key}] for UPDATE.", key);
                }
                else
                {
                    var entity = MapToEntity(dto, key);
                    _db.Tenders.Add(entity);
                    inserted++;
                    _logger.LogDebug("UpsertBatchAsync: marked [{Key}] for INSERT.", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "UpsertBatchAsync: failed to classify tender [{Key}] — skipping.", key);
                failed++;
            }
        }

        _logger.LogInformation(
            "UpsertBatchAsync: change-tracking summary before SaveChanges — " +
            "INSERT={Insert}, UPDATE={Update}, SKIP={Skip}.",
            inserted, updated, failed);

        if (inserted + updated == 0)
        {
            _logger.LogWarning("UpsertBatchAsync: nothing to save (all classified as failed/skipped).");
            return (0, 0, failed);
        }

        // ?? Step 4: save all tracked changes in one round-trip ????????????????
        int rowsAffected;
        try
        {
            _logger.LogDebug("UpsertBatchAsync: calling SaveChangesAsync...");
            rowsAffected = await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "UpsertBatchAsync: SaveChangesAsync completed — {Rows} row(s) written to SQL Server.",
                rowsAffected);
        }
        catch (DbUpdateException dbEx)
        {
            // Unwrap to get the inner SqlException for the exact SQL Server error number + message.
            var inner = dbEx.InnerException as SqlException;
            _logger.LogError(dbEx,
                "UpsertBatchAsync: DbUpdateException during SaveChangesAsync. " +
                "SQL error number={SqlError}, message={SqlMessage}. " +
                "Entries with errors: {EntryCount}.",
                inner?.Number,
                inner?.Message ?? dbEx.Message,
                dbEx.Entries.Count);

            foreach (var entry in dbEx.Entries)
            {
                _logger.LogError(
                    "  ? Failed entity type={Type}, state={State}",
                    entry.Entity.GetType().Name,
                    entry.State);
            }

            return (0, 0, dtos.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("UpsertBatchAsync: SaveChangesAsync was cancelled.");
            throw; // Let the job loop handle cancellation properly.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UpsertBatchAsync: unexpected exception during SaveChangesAsync.");
            return (0, 0, dtos.Count);
        }

        // ?? Step 5: post-save verification ????????????????????????????????????
        // Confirm the rows actually landed in SQL Server by counting them.
        await VerifyPersistedCountAsync(batchKeys, inserted, cancellationToken);

        return (inserted, updated, failed);
    }

    /// <summary>
    /// Convenience wrapper — same as <see cref="UpsertBatchAsync"/> but accepts
    /// a <see cref="List{T}"/> directly (matches the call-site signature in the job).
    /// </summary>
    public Task<(int Inserted, int Updated, int Failed)> UpsertRangeAsync(
        List<TenderDto> tenders,
        CancellationToken ct = default)
        => UpsertBatchAsync(tenders, ct);

    // =========================================================================
    // Private helpers
    // =========================================================================

    /// <summary>
    /// Queries the database for <paramref name="keys"/> after a save and logs
    /// the actual count so silent failures are immediately visible in the logs.
    /// </summary>
    private async Task VerifyPersistedCountAsync(
        List<string> keys,
        int expectedInserted,
        CancellationToken cancellationToken)
    {
        try
        {
            // Use a fresh, no-tracking query so EF doesn't return cached data.
            var dbCount = await _db.Tenders
                .AsNoTracking()
                .Where(t => keys.Contains(t.TenderNumber))
                .CountAsync(cancellationToken);

            _logger.LogInformation(
                "UpsertBatchAsync: POST-SAVE VERIFICATION — " +
                "keys in batch={Keys}, rows found in DB={DbCount}, expected new inserts={Expected}.",
                keys.Count, dbCount, expectedInserted);

            // Total rows in the table — useful to confirm data is visible.
            var totalInTable = await _db.Tenders.AsNoTracking().CountAsync(cancellationToken);
            _logger.LogInformation(
                "UpsertBatchAsync: total rows in [Tenders] table after save = {Total}.", totalInTable);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UpsertBatchAsync: post-save verification query failed (non-fatal).");
        }
    }

    /// <summary>Returns a non-null error message if the DTO should be rejected.</summary>
    private static string? ValidateDto(TenderDto dto)
    {
        if (dto is null)
            return "DTO is null";

        if (string.IsNullOrWhiteSpace(dto.Title) &&
            string.IsNullOrWhiteSpace(dto.TenderNumber) &&
            string.IsNullOrWhiteSpace(dto.DetailsUrl))
            return "Title, TenderNumber, and DetailsUrl are all empty";

        return null;
    }

    /// <summary>
    /// Returns the stable unique key for a tender.
    /// Priority: TenderNumber ? DetailsUrl.
    /// </summary>
    private static string ResolveBusinessKey(TenderDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.TenderNumber))
            return Truncate(dto.TenderNumber, MaxLen.TenderNumber);

        if (!string.IsNullOrWhiteSpace(dto.DetailsUrl))
            return Truncate(dto.DetailsUrl, MaxLen.TenderNumber);

        return string.Empty;
    }

    private static TenderEntity MapToEntity(TenderDto dto, string businessKey) => new()
    {
        TenderNumber   = businessKey,
        Title          = Truncate(dto.Title,          MaxLen.Title),
        Organization   = Truncate(dto.Organization,   MaxLen.Organization),
        PublishDate    = Truncate(dto.PublishDate,     MaxLen.PublishDate),
        ClosingDate    = Truncate(dto.ClosingDate,     MaxLen.ClosingDate),
        DetailsUrl     = Truncate(dto.DetailsUrl,      MaxLen.DetailsUrl),
        Category       = Truncate(dto.Category,        MaxLen.Category),
        Department     = Truncate(dto.Department,      MaxLen.Department),
        Status         = Truncate(dto.Status,          MaxLen.Status),
        AdditionalInfo = Truncate(dto.AdditionalInfo,  MaxLen.AdditionalInfo),
        FirstScrapedAt = DateTime.UtcNow,
        LastScrapedAt  = DateTime.UtcNow
    };

    private static void ApplyUpdate(TenderEntity entity, TenderDto dto)
    {
        entity.Title          = Truncate(dto.Title,          MaxLen.Title);
        entity.Organization   = Truncate(dto.Organization,   MaxLen.Organization);
        entity.PublishDate    = Truncate(dto.PublishDate,     MaxLen.PublishDate);
        entity.ClosingDate    = Truncate(dto.ClosingDate,     MaxLen.ClosingDate);
        entity.DetailsUrl     = Truncate(dto.DetailsUrl,      MaxLen.DetailsUrl);
        entity.Category       = Truncate(dto.Category,        MaxLen.Category);
        entity.Department     = Truncate(dto.Department,      MaxLen.Department);
        entity.Status         = Truncate(dto.Status,          MaxLen.Status);
        entity.AdditionalInfo = Truncate(dto.AdditionalInfo,  MaxLen.AdditionalInfo);
        entity.LastScrapedAt  = DateTime.UtcNow;
    }

    /// <summary>Safely clips a string to <paramref name="maxLength"/> characters.</summary>
    private static string Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) ? string.Empty
            : value.Length <= maxLength ? value
            : value[..maxLength];
}
