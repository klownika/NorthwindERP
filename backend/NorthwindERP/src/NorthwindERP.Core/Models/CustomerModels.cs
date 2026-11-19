namespace NorthwindERP.Core.Models;

/// <summary>Datos del cliente a actualizar. Los null o vacíos se ignoran.</summary>
public record CustomerContactUpdate(
    string? CustomerName,
    string? ContactName,
    string? Address,
    string? City,
    string? PostalCode,
    string? Country);
