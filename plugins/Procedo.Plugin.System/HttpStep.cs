using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class HttpStep : IProcedoStep
{
    private readonly HttpClient _httpClient;
    private readonly SystemSecurityGuard _securityGuard;

    public HttpStep()
        : this(new HttpClient(), null)
    {
    }

    public HttpStep(SystemPluginSecurityOptions? securityOptions)
        : this(new HttpClient(), securityOptions)
    {
    }

    public HttpStep(HttpClient httpClient, SystemPluginSecurityOptions? securityOptions = null)    {
        _httpClient = httpClient;
        _securityGuard = new SystemSecurityGuard(securityOptions);
    }

    public async Task<StepResult> ExecuteAsync(StepContext context)
    {
        try
        {
            var url = context.Inputs.TryGetValue("url", out var urlValue)
                ? SystemInputReader.GetString(urlValue)
                : string.Empty;

            if (string.IsNullOrWhiteSpace(url))
            {
                return new StepResult
                {
                    Success = false,
                    Error = "Input 'url' is required."
                };
            }

            var securityError = _securityGuard.EnsureHttpAllowed(url);
            if (securityError is not null)
            {
                return new StepResult
                {
                    Success = false,
                    Error = securityError
                };
            }

            var method = context.Inputs.TryGetValue("method", out var methodValue)
                ? SystemInputReader.GetString(methodValue, "GET")
                : "GET";

            var timeoutMs = context.Inputs.TryGetValue("timeout_ms", out var timeoutValue)
                ? Math.Max(1, SystemInputReader.GetInt(timeoutValue, 30000))
                : 30000;

            var allowNonSuccess = context.Inputs.TryGetValue("allow_non_success", out var allowValue)
                && SystemInputReader.GetBool(allowValue);

            var request = new HttpRequestMessage(new HttpMethod(method), url);

            if (context.Inputs.TryGetValue("headers", out var headersValue))
            {
                foreach (var (key, value) in SystemInputReader.GetDictionary(headersValue))
                {
                    request.Headers.TryAddWithoutValidation(key, SystemInputReader.GetString(value));
                }
            }

            if (context.Inputs.TryGetValue("body", out var bodyValue))
            {
                if (bodyValue is string textBody)
                {
                    var contentType = context.Inputs.TryGetValue("content_type", out var ctValue)
                        ? SystemInputReader.GetString(ctValue, "text/plain")
                        : "text/plain";

                    request.Content = new StringContent(textBody, Encoding.UTF8, contentType);
                }
                else
                {
                    var json = JsonSerializer.Serialize(bodyValue);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }
            }

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
            linkedCts.CancelAfter(timeoutMs);

            using var response = await _httpClient.SendAsync(request, linkedCts.Token).ConfigureAwait(false);

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var headers = FlattenHeaders(response.Headers, response.Content.Headers);

            if (!response.IsSuccessStatusCode && !allowNonSuccess)
            {
                return new StepResult
                {
                    Success = false,
                    Error = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}",
                    Outputs = new Dictionary<string, object>
                    {
                        ["status_code"] = (int)response.StatusCode,
                        ["reason_phrase"] = response.ReasonPhrase ?? string.Empty,
                        ["is_success"] = response.IsSuccessStatusCode,
                        ["body"] = responseBody,
                        ["headers"] = headers
                    }
                };
            }

            return new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["status_code"] = (int)response.StatusCode,
                    ["reason_phrase"] = response.ReasonPhrase ?? string.Empty,
                    ["is_success"] = response.IsSuccessStatusCode,
                    ["body"] = responseBody,
                    ["headers"] = headers
                }
            };
        }
        catch (Exception ex)
        {
            return new StepResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private static IDictionary<string, object> FlattenHeaders(HttpResponseHeaders responseHeaders, HttpContentHeaders contentHeaders)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in responseHeaders)
        {
            result[header.Key] = string.Join(",", header.Value);
        }

        foreach (var header in contentHeaders)
        {
            result[header.Key] = string.Join(",", header.Value);
        }

        return result;
    }
}

