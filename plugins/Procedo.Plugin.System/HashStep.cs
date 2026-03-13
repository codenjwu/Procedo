using System.Security.Cryptography;
using System.Text;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class HashStep : IProcedoStep
{
    private readonly SystemSecurityGuard _securityGuard;

    public HashStep(SystemPluginSecurityOptions? securityOptions = null)
    {
        _securityGuard = new SystemSecurityGuard(securityOptions);
    }

    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var algorithm = context.Inputs.TryGetValue("algorithm", out var algorithmValue)
            ? SystemInputReader.GetString(algorithmValue, "SHA256")
            : "SHA256";

        byte[] bytes;
        if (context.Inputs.TryGetValue("file_path", out var fileValue))
        {
            var filePath = SystemInputReader.GetString(fileValue);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult(new StepResult
                {
                    Success = false,
                    Error = "Input 'file_path' is required when hashing a file."
                });
            }

            var pathError = _securityGuard.EnsurePathAllowed(filePath, "file_path");
            if (pathError is not null)
            {
                return Task.FromResult(new StepResult { Success = false, Error = pathError });
            }

            if (!File.Exists(filePath))
            {
                return Task.FromResult(new StepResult
                {
                    Success = false,
                    Error = $"File '{filePath}' does not exist."
                });
            }

            bytes = File.ReadAllBytes(filePath);
        }
        else
        {
            var text = context.Inputs.TryGetValue("text", out var textValue)
                ? SystemInputReader.GetString(textValue)
                : string.Empty;
            bytes = Encoding.UTF8.GetBytes(text);
        }

        var hash = ComputeHash(bytes, algorithm);
        if (hash is null)
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = $"Unsupported algorithm '{algorithm}'."
            });
        }

        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["value"] = hex,
                ["algorithm"] = algorithm.ToUpperInvariant()
            }
        });
    }

    private static byte[]? ComputeHash(byte[] bytes, string algorithm)
    {
        using HashAlgorithm? hash = algorithm.ToUpperInvariant() switch
        {
            "MD5" => MD5.Create(),
            "SHA1" => SHA1.Create(),
            "SHA256" => SHA256.Create(),
            "SHA384" => SHA384.Create(),
            "SHA512" => SHA512.Create(),
            _ => null
        };

        return hash?.ComputeHash(bytes);
    }
}
