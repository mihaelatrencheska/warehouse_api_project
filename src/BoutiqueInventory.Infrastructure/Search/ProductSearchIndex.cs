using System.Text;
using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Domain.Entities;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace BoutiqueInventory.Infrastructure.Search;

/// <summary>SQLite FTS5 shadow table kept in sync with <see cref="Product"/> rows.</summary>
public sealed class ProductSearchIndex(IConfiguration configuration) : IProductSearchIndex
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

    private const string CreateTableSql = """
        CREATE VIRTUAL TABLE IF NOT EXISTS ProductsFts USING fts5(
            ProductId UNINDEXED,
            Name,
            Sku,
            Description,
            tokenize = 'unicode61 remove_diacritics 2'
        );
        """;

    public async Task EnsureSchemaAsync(CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(CreateTableSql, cancellationToken: ct));
    }

    public async Task<bool> IsEmptyAsync(CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(CreateTableSql, cancellationToken: ct));
        var count = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition("SELECT COUNT(*) FROM ProductsFts;", cancellationToken: ct));
        return count == 0;
    }

    public async Task RebuildAsync(CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(CreateTableSql, cancellationToken: ct));
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM ProductsFts;", cancellationToken: ct));

        const string selectSql = "SELECT Id, Name, Sku, Description FROM Products;";

        var rows = await connection.QueryAsync<(Guid Id, string Name, string Sku, string? Description)>(
            new CommandDefinition(selectSql, cancellationToken: ct));

        foreach (var row in rows)
        {
            await UpsertInternalAsync(connection, row.Id, row.Name, row.Sku, row.Description, ct);
        }
    }

    public async Task UpsertAsync(Product product, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(CreateTableSql, cancellationToken: ct));
        await UpsertInternalAsync(connection, product.Id, product.Name, product.Sku, product.Description, ct);
    }

    public async Task DeleteAsync(Guid productId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM ProductsFts WHERE ProductId = @ProductId;",
            new { ProductId = productId.ToString() },
            cancellationToken: ct));
    }

    internal static string ToFtsMatchQuery(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var tokens = raw.Trim()
            .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length == 0) return string.Empty;

        var parts = new List<string>(tokens.Length);
        foreach (var token in tokens)
        {
            var escaped = EscapeFtsToken(token);
            if (escaped.Length > 0)
            {
                parts.Add($"{escaped}*");
            }
        }

        return parts.Count == 0 ? string.Empty : string.Join(" OR ", parts);
    }

    private static string EscapeFtsToken(string token)
    {
        var sb = new StringBuilder(token.Length);
        foreach (var ch in token)
        {
            if (char.IsLetterOrDigit(ch) || ch is '-' or '_')
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    private static async Task UpsertInternalAsync(
        SqliteConnection connection,
        Guid productId,
        string name,
        string sku,
        string? description,
        CancellationToken ct)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM ProductsFts WHERE ProductId = @ProductId;",
            new { ProductId = productId.ToString() },
            cancellationToken: ct));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO ProductsFts (ProductId, Name, Sku, Description)
            VALUES (@ProductId, @Name, @Sku, @Description);
            """,
            new
            {
                ProductId = productId.ToString(),
                Name = name,
                Sku = sku,
                Description = description ?? string.Empty
            },
            cancellationToken: ct));
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
