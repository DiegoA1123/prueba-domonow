namespace DomoNow.Parking.Infrastructure.Identity;

/// <summary>Usuario autorizado (subdominio genérico de identidad, ADR-0004). No pertenece al dominio Parking.</summary>
public sealed class Operator
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Username { get; init; }
    public string PasswordHash { get; set; } = string.Empty;
    public required string Role { get; init; }
}
