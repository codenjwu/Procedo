using System.Collections.Generic;
using System.Linq;

namespace Procedo.Validation.Models;

public sealed class ValidationResult
{
    public List<ValidationIssue> Issues { get; } = new();

    public bool HasErrors => Issues.Any(i => i.Severity == ValidationSeverity.Error);

    public bool HasWarnings => Issues.Any(i => i.Severity == ValidationSeverity.Warning);

    public IReadOnlyList<ValidationIssue> Errors
        => Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();

    public IReadOnlyList<ValidationIssue> Warnings
        => Issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();

    public void AddError(string code, string message, string path, string? sourcePath = null)
    {
        Issues.Add(new ValidationIssue
        {
            Severity = ValidationSeverity.Error,
            Code = code,
            Message = message,
            Path = path,
            SourcePath = sourcePath
        });
    }

    public void AddWarning(string code, string message, string path, string? sourcePath = null)
    {
        Issues.Add(new ValidationIssue
        {
            Severity = ValidationSeverity.Warning,
            Code = code,
            Message = message,
            Path = path,
            SourcePath = sourcePath
        });
    }
}
