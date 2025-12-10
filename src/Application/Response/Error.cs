namespace Application.Response;

public sealed record Error(int Index, string Field, string Message);