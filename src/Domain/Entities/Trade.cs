namespace Domain.Entities;

public sealed record Trade(decimal Value, string ClientSector, string? ClientId = null);
