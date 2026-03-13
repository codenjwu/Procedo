namespace Procedo.Validation.Models;

public sealed class ValidationIssue
{
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;

    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string? SourcePath { get; set; }
}
