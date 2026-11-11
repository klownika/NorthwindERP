using Npgsql;
using NpgsqlTypes;

namespace NorthwindERP.Infrastructure.Data;

/// <summary>Utilidades sobre Npgsql. PostgreSQL devuelve los nombres de columna en minúsculas.</summary>
internal static class DbHelpers
{
    /// <summary>Parámetro con tipo explícito, necesario para que "@p IS NULL" funcione con valores null.</summary>
    public static NpgsqlParameter P(string name, object? value, NpgsqlDbType type)
        => new(name, type) { Value = value ?? DBNull.Value };

    public static T Get<T>(this NpgsqlDataReader r, string column)
        => r.GetFieldValue<T>(r.GetOrdinal(column));

    public static string? GetStringOrNull(this NpgsqlDataReader r, string column)
    {
        var i = r.GetOrdinal(column);
        return r.IsDBNull(i) ? null : r.GetFieldValue<string>(i);
    }

    public static int? GetIntOrNull(this NpgsqlDataReader r, string column)
    {
        var i = r.GetOrdinal(column);
        return r.IsDBNull(i) ? null : r.GetFieldValue<int>(i);
    }

    public static async Task<IReadOnlyList<T>> QueryAsync<T>(
        this NpgsqlDataSource ds,
        string sql,
        Func<NpgsqlDataReader, T> map,
        CancellationToken ct,
        params NpgsqlParameter[] parameters)
    {
        await using var cmd = ds.CreateCommand(sql);
        foreach (var p in parameters) cmd.Parameters.Add(p);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<T>();
        while (await reader.ReadAsync(ct)) list.Add(map(reader));
        return list;
    }
}
