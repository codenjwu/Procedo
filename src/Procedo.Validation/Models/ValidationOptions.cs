namespace Procedo.Validation.Models;

public sealed class ValidationOptions
{
    public static ValidationOptions Permissive { get; } = new();

    public static ValidationOptions Strict { get; } = new() { TreatWarningsAsErrors = true };

    public bool TreatWarningsAsErrors { get; set; }
}
