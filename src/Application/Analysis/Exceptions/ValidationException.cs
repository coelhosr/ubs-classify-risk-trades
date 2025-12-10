using Application.Response;

namespace Application.Analysis.Exceptions;

public class ValidationException : Exception
{
    public IReadOnlyList<Error> Errors { get; }
    public ValidationException(IEnumerable<Error> errors) : base("Erros de validação") => Errors = errors is List<Error> list ? list : [.. errors];
}
