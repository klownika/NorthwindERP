namespace NorthwindERP.Core.Constants;

// Tipos de movimiento del Kardex. Son los únicos valores que acepta
// el CHECK de la columna InventoryTransactions.TransactionType.
public static class TransactionTypes
{
    public const string Inbound = "INBOUND";
    public const string Outbound = "OUTBOUND";
}
