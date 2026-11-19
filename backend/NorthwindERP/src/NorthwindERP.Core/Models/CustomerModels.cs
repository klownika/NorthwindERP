namespace NorthwindERP.Core.Models;

/// <summary>Campos de dirección a actualizar. Los null se ignoran.</summary>
public record CustomerContactUpdate(string? Address, string? City, string? PostalCode, string? Country);
