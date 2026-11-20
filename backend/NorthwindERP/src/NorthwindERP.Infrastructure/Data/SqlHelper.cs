using Npgsql;
using NpgsqlTypes;

namespace NorthwindERP.Infrastructure.Data;

// Funciones pequeñas para no repetir código cuando usamos SQL directo con Npgsql.
// Ojo: PostgreSQL devuelve los nombres de columna en MINÚSCULAS, por eso
// siempre se leen como "customerid", "productname", etc.
public static class SqlHelper
{
    // Crea un parámetro (@nombre) con su tipo. El tipo es obligatorio para que
    // los filtros "(@p IS NULL OR columna = @p)" funcionen cuando el valor es null.
    public static NpgsqlParameter CreateParameter(string name, object? value, NpgsqlDbType type)
    {
        NpgsqlParameter parameter = new NpgsqlParameter(name, type);

        if (value == null)
        {
            parameter.Value = DBNull.Value;
        }
        else
        {
            parameter.Value = value;
        }

        return parameter;
    }

    public static int ReadInt(NpgsqlDataReader reader, string column)
    {
        int index = reader.GetOrdinal(column);
        return reader.GetFieldValue<int>(index);
    }

    public static int? ReadNullableInt(NpgsqlDataReader reader, string column)
    {
        int index = reader.GetOrdinal(column);

        if (reader.IsDBNull(index))
        {
            return null;
        }

        return reader.GetFieldValue<int>(index);
    }

    public static string ReadString(NpgsqlDataReader reader, string column)
    {
        int index = reader.GetOrdinal(column);
        return reader.GetFieldValue<string>(index);
    }

    public static string? ReadNullableString(NpgsqlDataReader reader, string column)
    {
        int index = reader.GetOrdinal(column);

        if (reader.IsDBNull(index))
        {
            return null;
        }

        return reader.GetFieldValue<string>(index);
    }

    public static decimal ReadDecimal(NpgsqlDataReader reader, string column)
    {
        int index = reader.GetOrdinal(column);
        return reader.GetFieldValue<decimal>(index);
    }

    public static bool ReadBool(NpgsqlDataReader reader, string column)
    {
        int index = reader.GetOrdinal(column);
        return reader.GetFieldValue<bool>(index);
    }

    public static DateTime ReadDateTime(NpgsqlDataReader reader, string column)
    {
        int index = reader.GetOrdinal(column);
        return reader.GetFieldValue<DateTime>(index);
    }
}
