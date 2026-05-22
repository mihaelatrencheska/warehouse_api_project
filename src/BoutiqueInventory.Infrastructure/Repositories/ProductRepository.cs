using System.Data;
using System.Text;
using BoutiqueInventory.Application.Common;
using BoutiqueInventory.Application.DTOs.Requests;
using BoutiqueInventory.Application.DTOs.Responses;
using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Domain.Entities;
using BoutiqueInventory.Infrastructure.Data;
using BoutiqueInventory.Infrastructure.Search;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BoutiqueInventory.Infrastructure.Repositories;

/// <inheritdoc cref="IProductRepository"/>
public sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetByIdWithGraphAsync(Guid id, CancellationToken ct) =>
        db.Products
            .Include(p => p.WarehouseSection).ThenInclude(s => s.Warehouse)
            .Include(p => p.Categories).ThenInclude(pc => pc.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> SkuTakenAsync(string sku, Guid? exceptId, CancellationToken ct) =>
        db.Products.AsNoTracking().AnyAsync(p =>
            p.Sku == sku && (exceptId == null || p.Id != exceptId), ct);

    public async Task<IReadOnlyList<Product>> ListByWarehouseAsync(Guid warehouseId, CancellationToken ct)
    {
        var ids = await db.WarehouseSections.AsNoTracking()
            .Where(s => s.WarehouseId == warehouseId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (ids.Count == 0) return Array.Empty<Product>();

        return await db.Products.AsNoTracking()
            .Include(p => p.WarehouseSection).ThenInclude(s => s.Warehouse)
            .Include(p => p.Categories).ThenInclude(pc => pc.Category)
            .Where(p => ids.Contains(p.WarehouseSectionId))
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Uses Dapper because SQLite's EF provider cannot translate
    /// DateTimeOffset comparisons. Returns hydrated <see cref="Product"/>
    /// instances so the caller can map without re-querying.
    /// </summary>
    public async Task<IReadOnlyList<Product>> ListExpiringWithinAsync(int days, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var threshold = now.AddDays(days);

        const string sql = @"
SELECT  p.Id, p.Name, p.Sku, p.Description, p.Size, p.Type,
        p.ExpirationDate, p.ImageUrl, p.ImageMetadata,
        p.WarehouseSectionId, p.CreatedAt, p.UpdatedAt,
        s.Id, s.WarehouseId, s.Name,
        w.Id, w.Name, w.Location, w.IsActive, w.CreatedAt, w.DeactivatedAt
FROM Products p
JOIN WarehouseSections s ON s.Id = p.WarehouseSectionId
JOIN Warehouses w        ON w.Id = s.WarehouseId
WHERE p.ExpirationDate IS NOT NULL
  AND p.ExpirationDate >= @Now
  AND p.ExpirationDate <= @Threshold
ORDER BY p.ExpirationDate;";

        var connection = await OpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<Product, WarehouseSection, Warehouse, Product>(
            new CommandDefinition(sql, new { Now = now, Threshold = threshold }, cancellationToken: ct),
            (product, section, warehouse) =>
            {
                section.Warehouse = warehouse;
                product.WarehouseSection = section;
                return product;
            },
            splitOn: "Id,Id");

        return rows.AsList();
    }

    public async Task<IReadOnlyList<Product>> ListWithImageFingerprintAsync(CancellationToken ct) =>
        await db.Products.AsNoTracking()
            .Include(p => p.WarehouseSection).ThenInclude(s => s.Warehouse)
            .Where(p => p.ImageMetadata != null && p.ImageMetadata != "")
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProductImageFingerprintRow>> ListImageFingerprintRowsAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT p.Id,
                   p.Name,
                   p.Sku,
                   p.Size,
                   p.Type,
                   p.ExpirationDate,
                   p.ImageUrl,
                   w.Id   AS WarehouseId,
                   w.Name AS WarehouseName,
                   s.Id   AS SectionId,
                   s.Name AS SectionName,
                   json_extract(p.ImageMetadata, '$.hashHex') AS HashHex
            FROM Products p
            JOIN WarehouseSections s ON s.Id = p.WarehouseSectionId
            JOIN Warehouses w        ON w.Id = s.WarehouseId
            WHERE p.ImageMetadata IS NOT NULL
              AND p.ImageMetadata != ''
              AND json_extract(p.ImageMetadata, '$.hashHex') IS NOT NULL
            ORDER BY p.Name;
            """;

        var connection = await OpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<ProductImageFingerprintRow>(
            new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<PagedResult<ProductSummaryResponse>> ListPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var countSql = """
            SELECT COUNT(*)
            FROM Products p
            JOIN WarehouseSections s ON s.Id = p.WarehouseSectionId
            JOIN Warehouses w        ON w.Id = s.WarehouseId;
            """;

        const string pageSql = """
            SELECT p.Id          AS Id,
                   p.Name        AS Name,
                   p.Sku         AS Sku,
                   p.Size        AS Size,
                   p.Type        AS Type,
                   p.ExpirationDate AS ExpirationDate,
                   p.ImageUrl    AS ImageUrl,
                   w.Id          AS WarehouseId,
                   w.Name        AS WarehouseName,
                   s.Id          AS SectionId,
                   s.Name        AS SectionName,
                   (SELECT GROUP_CONCAT(c.Name, ', ')
                    FROM ProductCategories pc
                    JOIN Categories c ON c.Id = pc.CategoryId
                    WHERE pc.ProductId = p.Id) AS CategoryNames
            FROM Products p
            JOIN WarehouseSections s ON s.Id = p.WarehouseSectionId
            JOIN Warehouses w        ON w.Id = s.WarehouseId
            ORDER BY p.Name COLLATE NOCASE
            LIMIT @Take OFFSET @Skip;
            """;

        var parameters = new DynamicParameters();
        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        var connection = await OpenConnectionAsync(ct);
        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));
        var rows = await connection.QueryAsync<ProductSummaryResponse>(
            new CommandDefinition(pageSql, parameters, cancellationToken: ct));

        return new PagedResult<ProductSummaryResponse>(rows.AsList(), totalCount, page, pageSize);
    }

    public void Add(Product product) => db.Products.Add(product);
    public void Update(Product product) => db.Products.Update(product);
    public void Remove(Product product) => db.Products.Remove(product);

    /// <summary>
    /// Performs the search using Dapper against the same SQLite
    /// connection that EF Core opens for this scope. The query is fully
    /// parameterized — no string concatenation of user input.
    /// </summary>
    public async Task<PagedResult<ProductSummaryResponse>> SearchAsync(
        ProductSearchRequest request,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var (whereSql, parameters, ftsJoin) = BuildWhereClause(request);

        var countSql = $@"
SELECT COUNT(*)
FROM Products p
JOIN WarehouseSections s ON s.Id = p.WarehouseSectionId
JOIN Warehouses w        ON w.Id = s.WarehouseId
{ftsJoin}
{whereSql};";

        var pageSql = $@"
SELECT p.Id          AS Id,
       p.Name        AS Name,
       p.Sku         AS Sku,
       p.Size        AS Size,
       p.Type        AS Type,
       p.ExpirationDate AS ExpirationDate,
       p.ImageUrl    AS ImageUrl,
       w.Id          AS WarehouseId,
       w.Name        AS WarehouseName,
       s.Id          AS SectionId,
       s.Name        AS SectionName,
       (SELECT GROUP_CONCAT(c.Name, ', ')
        FROM ProductCategories pc
        JOIN Categories c ON c.Id = pc.CategoryId
        WHERE pc.ProductId = p.Id) AS CategoryNames
FROM Products p
JOIN WarehouseSections s ON s.Id = p.WarehouseSectionId
JOIN Warehouses w        ON w.Id = s.WarehouseId
{ftsJoin}
{whereSql}
ORDER BY p.Name COLLATE NOCASE
LIMIT @Take OFFSET @Skip;";

        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        var connection = await OpenConnectionAsync(ct);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        var rows = await connection.QueryAsync<ProductSummaryResponse>(
            new CommandDefinition(pageSql, parameters, cancellationToken: ct));

        return new PagedResult<ProductSummaryResponse>(rows.AsList(), totalCount, page, pageSize);
    }

    public async Task<int> CountExpiredAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM Products
            WHERE ExpirationDate IS NOT NULL
              AND ExpirationDate < @Now;
            """;

        var connection = await OpenConnectionAsync(ct);
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Now = DateTimeOffset.UtcNow }, cancellationToken: ct));
    }

    private async Task<IDbConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await ((SqliteConnection)conn).OpenAsync(ct);
        }
        return conn;
    }

    private static (string Sql, DynamicParameters Parameters, string FtsJoin) BuildWhereClause(
        ProductSearchRequest request)
    {
        var parameters = new DynamicParameters();
        var clauses = new List<string>();
        var ftsJoin = string.Empty;

        var ftsQuery = ProductSearchIndex.ToFtsMatchQuery(request.Query);
        if (!string.IsNullOrEmpty(ftsQuery))
        {
            ftsJoin = "JOIN ProductsFts fts ON fts.ProductId = CAST(p.Id AS TEXT)";
            clauses.Add("fts MATCH @FtsQuery");
            parameters.Add("FtsQuery", ftsQuery);
        }
        else if (!string.IsNullOrWhiteSpace(request.Query))
        {
            clauses.Add("(p.Name LIKE @Query OR p.Sku LIKE @Query OR p.Description LIKE @Query)");
            parameters.Add("Query", $"%{request.Query.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(request.Size))
        {
            clauses.Add("p.Size = @Size");
            parameters.Add("Size", request.Size.Trim());
        }
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            clauses.Add("p.Type = @Type");
            parameters.Add("Type", request.Type.Trim());
        }
        if (request.WarehouseId is { } warehouseId)
        {
            clauses.Add("w.Id = @WarehouseId COLLATE NOCASE");
            parameters.Add("WarehouseId", warehouseId.ToString());
        }
        if (request.SectionId is { } sectionId)
        {
            clauses.Add("s.Id = @SectionId COLLATE NOCASE");
            parameters.Add("SectionId", sectionId.ToString());
        }
        if (request.CategoryId is { } categoryId)
        {
            clauses.Add(@"EXISTS (
                SELECT 1 FROM ProductCategories pc
                WHERE pc.ProductId = p.Id COLLATE NOCASE
                  AND pc.CategoryId = @CategoryId COLLATE NOCASE
            )");
            parameters.Add("CategoryId", categoryId.ToString());
        }
        if (request.ExpiringWithinDays is > 0)
        {
            clauses.Add(
                "p.ExpirationDate IS NOT NULL AND p.ExpirationDate >= @ExpiresAfter AND p.ExpirationDate <= @ExpiresBefore");
            parameters.Add("ExpiresAfter", DateTimeOffset.UtcNow);
            parameters.Add("ExpiresBefore", DateTimeOffset.UtcNow.AddDays(request.ExpiringWithinDays.Value));
        }

        if (clauses.Count == 0) return (string.Empty, parameters, ftsJoin);

        var sb = new StringBuilder("WHERE ");
        sb.AppendJoin(" AND ", clauses);
        return (sb.ToString(), parameters, ftsJoin);
    }
}
