using Procedo.Validation.Models;

namespace Procedo.Engine.Hosting;

public sealed class ProcedoValidationException : Exception
{
    public ProcedoValidationException(string message, ValidationResult validationResult)
        : base(message)
    {
        ValidationResult = validationResult;
    }

    public ValidationResult ValidationResult { get; }
}
