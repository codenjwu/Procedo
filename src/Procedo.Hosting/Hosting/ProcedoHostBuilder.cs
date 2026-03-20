using Procedo.Core.Abstractions;
using Procedo.Plugin.SDK;

namespace Procedo.Engine.Hosting;

public sealed class ProcedoHostBuilder
{
    private readonly IPluginRegistry _pluginRegistry = new PluginRegistry();
    private readonly ProcedoHostOptions _options = new();

    public ProcedoHostBuilder Configure(Action<ProcedoHostOptions> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        configure(_options);
        return this;
    }

    public ProcedoHostBuilder ConfigurePlugins(Action<IPluginRegistry> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        configure(_pluginRegistry);
        return this;
    }

    public ProcedoHostBuilder UseServiceProvider(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        _pluginRegistry.UseServiceProvider(serviceProvider);
        return this;
    }

    public ProcedoHostBuilder ConfigureExecution(Action<Core.Execution.WorkflowExecutionOptions> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        configure(_options.Execution);
        return this;
    }

    public ProcedoHostBuilder ConfigureValidation(Action<Validation.Models.ValidationOptions> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        configure(_options.Validation);
        return this;
    }

    public ProcedoHostBuilder UseLogger(ILogger logger)
    {
        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        _options.Logger = logger;
        return this;
    }

    public ProcedoHostBuilder UseEventSink(Observability.IExecutionEventSink? sink)
    {
        _options.EventSink = sink;
        return this;
    }

    public ProcedoHostBuilder UseRunStateStore(IRunStateStore store, string? resumeRunId = null)
    {
        if (store is null)
        {
            throw new ArgumentNullException(nameof(store));
        }

        _options.RunStateStore = store;
        _options.ResumeRunId = resumeRunId;
        return this;
    }

    public ProcedoHostBuilder UseWorkflowDefinitionResolver(IWorkflowDefinitionResolver resolver)
    {
        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        _options.WorkflowDefinitionResolver = resolver;
        return this;
    }

    public ProcedoHostBuilder UseLocalRunStateStore(string directoryPath, string? resumeRunId = null)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("A persistence directory path is required.", nameof(directoryPath));
        }

        _options.WorkflowDefinitionResolver ??= new FileWorkflowDefinitionResolver();
        return UseRunStateStore(new Persistence.Stores.FileRunStateStore(directoryPath), resumeRunId);
    }

    public ProcedoHost Build()
    {
        ValidateOptions();
        return new ProcedoHost(_pluginRegistry, _options);
    }

    private void ValidateOptions()
    {
        if (_options.Logger is null)
        {
            throw new InvalidOperationException("A logger must be configured before building a Procedo host.");
        }

        if (_options.Parser is null)
        {
            throw new InvalidOperationException("A workflow parser must be configured before building a Procedo host.");
        }

        if (!string.IsNullOrWhiteSpace(_options.ResumeRunId) && _options.RunStateStore is null)
        {
            throw new InvalidOperationException("A run state store is required when a resume run id is configured.");
        }
    }
}
