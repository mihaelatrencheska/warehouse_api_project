using System.Data;
using System.Globalization;
using Dapper;

namespace BoutiqueInventory.Infrastructure.Data.Dapper;

/// <summary>
/// SQLite stores <see cref="Guid"/> values as TEXT (the canonical
/// hyphenated UUID form). Dapper does not parse TEXT → Guid by default,
/// so this handler bridges the gap.
/// </summary>
internal sealed class SqliteGuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value) => value switch
    {
        Guid g => g,
        string s => Guid.Parse(s),
        byte[] bytes when bytes.Length == 16 => new Guid(bytes),
        _ => throw new InvalidCastException($"Cannot convert '{value?.GetType().Name ?? "null"}' to Guid.")
    };

    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value.ToString();
    }
}

/// <summary>Same as <see cref="SqliteGuidTypeHandler"/> but for nullable Guid columns.</summary>
internal sealed class SqliteNullableGuidTypeHandler : SqlMapper.TypeHandler<Guid?>
{
    public override Guid? Parse(object? value) => value switch
    {
        null => null,
        DBNull => null,
        Guid g => g,
        string s when string.IsNullOrEmpty(s) => null,
        string s => Guid.Parse(s),
        byte[] bytes when bytes.Length == 16 => new Guid(bytes),
        _ => throw new InvalidCastException($"Cannot convert '{value.GetType().Name}' to Guid?.")
    };

    public override void SetValue(IDbDataParameter parameter, Guid? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value?.ToString() ?? (object)DBNull.Value;
    }
}

/// <summary>
/// SQLite stores <see cref="DateTimeOffset"/> as ISO-ish TEXT
/// ("yyyy-MM-dd HH:mm:ss.fffffff+zz:zz"). Dapper can't cast TEXT to
/// DateTimeOffset directly; this handler does it.
/// </summary>
internal sealed class SqliteDateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override DateTimeOffset Parse(object value) => value switch
    {
        DateTimeOffset dto => dto,
        DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc), TimeSpan.Zero),
        string s => DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
        _ => throw new InvalidCastException($"Cannot convert '{value?.GetType().Name ?? "null"}' to DateTimeOffset.")
    };

    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value.ToString("yyyy-MM-dd HH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture);
    }
}

/// <summary>Same as <see cref="SqliteDateTimeOffsetTypeHandler"/> for nullable columns.</summary>
internal sealed class SqliteNullableDateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset?>
{
    public override DateTimeOffset? Parse(object? value) => value switch
    {
        null => null,
        DBNull => null,
        DateTimeOffset dto => dto,
        DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc), TimeSpan.Zero),
        string s when string.IsNullOrEmpty(s) => null,
        string s => DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
        _ => throw new InvalidCastException($"Cannot convert '{value.GetType().Name}' to DateTimeOffset?.")
    };

    public override void SetValue(IDbDataParameter parameter, DateTimeOffset? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value?.ToString("yyyy-MM-dd HH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture)
            ?? (object)DBNull.Value;
    }
}
