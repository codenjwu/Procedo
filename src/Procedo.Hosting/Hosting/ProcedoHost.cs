using Procedo.Core.Models;
using Procedo.Core.Runtime;
using Procedo.DSL;
using Procedo.Plugin.SDK;
using Procedo.Validation;

namespace Procedo.Engine.Hosting;

public sealed class ProcedoHost
{
    private readonly ProcedoWorkflowEngine _engine = new();
    private readonly IPluginRegistry _pluginRegistry;
    private readonly ProcedoHostOptions _options;

    internal ProcedoHost(IPluginRegistry pluginRegistry, ProcedoHostOptions options)
    {
        _pluginRegistry = pluginRegistry;
        _options = options;
    }

    public Task<WorkflowRunResult> ExecuteYamlAsync(string yamlText, CancellationToken cancellationToken = default)
        => ExecuteYamlAsync(yamlText, baseDirectory: null, parameters: null, cancellationToken);

    public Task<WorkflowRunResult> ExecuteYamlAsync(
        string yamlText,
        string? baseDirectory,
        IDictionary<string, object>? parameters,
        CancellationToken cancellationToken = default)
    {
        var workflow = new WorkflowTemplateLoader().LoadFromText(yamlText, baseDirectory, parameters);
        return ExecuteWorkflowAsync(workflow, cancellationToken);
    }

    public Task<WorkflowRunResult> ExecuteFileAsync(
        string workflowPath,
        IDictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var workflow = new WorkflowTemplateLoader().LoadFromFile(workflowPath, parameters);
        return ExecuteWorkflowAsync(workflow, cancellationToken);
    }

    public async Task<WorkflowRunResult> ExecuteWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default)
    {
        ValidateWorkflow(workflow);

        if (_options.RunStateStore is not null)
        {
            return await _engine.ExecuteWithPersistenceAsync(
                workflow,
                _pluginRegistry,
                _options.Logger,
                _options.RunStateStore,
                _options.ResumeRunId,
                _options.EventSink,
                cancellationToken,
                _options.Execution).ConfigureAwait(false);
        }

        return await _engine.ExecuteAsync(
            workflow,
            _pluginRegistry,
            _options.Logger,
            _options.EventSink,
            cancellationToken,
            _options.Execution).ConfigureAwait(false);
    }

    public Task<WorkflowRunResult> ResumeYamlAsync(string yamlText, ResumeRequest request, CancellationToken cancellationToken = default)
        => ResumeYamlAsync(yamlText, request, baseDirectory: null, parameters: null, cancellationToken);

    public Task<WorkflowRunResult> ResumeYamlAsync(
        string yamlText,
        ResumeRequest request,
        string? baseDirectory,
        IDictionary<string, object>? parameters,
        CancellationToken cancellationToken = default)
    {
        var workflow = new WorkflowTemplateLoader().LoadFromText(yamlText, baseDirectory, parameters);
        return ResumeWorkflowAsync(workflow, request, cancellationToken);
    }

    public Task<WorkflowRunResult> ResumeFileAsync(
        string workflowPath,
        ResumeRequest request,
        IDictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var workflow = new WorkflowTemplateLoader().LoadFromFile(workflowPath, parameters);
        return ResumeWorkflowAsync(workflow, request, cancellationToken);
    }

    public async Task<WorkflowRunResult> ResumeWorkflowAsync(
        WorkflowDefinition workflow,
        ResumeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateWorkflow(workflow);

        if (_options.RunStateStore is null)
        {
            throw new InvalidOperationException("Resume requires a configured run state store.");
        }

        if (string.IsNullOrWhiteSpace(_options.ResumeRunId))
        {
            throw new InvalidOperationException("Resume requires a configured run id.");
        }

        return await _engine.ResumeAsync(
            workflow,
            _pluginRegistry,
            _options.Logger,
            _options.RunStateStore,
            _options.ResumeRunId,
            request,
            _options.EventSink,
            cancellationToken,
            _options.Execution).ConfigureAwait(false);
    }

    private void ValidateWorkflow(WorkflowDefinition workflow)
    {
        if (!_options.SkipValidation)
        {
            var validation = new ProcedoWorkflowValidator().Validate(workflow, _pluginRegistry, _options.Validation);
            if (validation.HasErrors)
            {
                throw new ProcedoValidationException("Workflow validation failed.", validation);
            }
        }
    }
}
