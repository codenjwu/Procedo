using Procedo.Core.Models;

namespace Procedo.DSL;

public sealed class WorkflowTemplateLoader
{
    private readonly YamlWorkflowParser _parser;

    public WorkflowTemplateLoader(YamlWorkflowParser? parser = null)
    {
        _parser = parser ?? new YamlWorkflowParser();
    }

    public WorkflowDefinition LoadFromFile(string path, IDictionary<string, object>? parameterOverrides = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A workflow file path is required.", nameof(path));
        }

        var fullPath = Path.GetFullPath(path);
        return LoadFromFileInternal(fullPath, new HashSet<string>(StringComparer.OrdinalIgnoreCase), parameterOverrides);
    }

    public WorkflowDefinition LoadFromText(string yamlText, string? baseDirectory = null, IDictionary<string, object>? parameterOverrides = null)
    {
        var workflow = _parser.Parse(yamlText);
        if (string.IsNullOrWhiteSpace(workflow.Template))
        {
            var clone = CloneWorkflow(workflow);
            StampLocalSources(clone, "<inline>");
            return ApplyParameterOverrides(clone, parameterOverrides, "<inline>");
        }

        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            throw new InvalidOperationException("Template workflows loaded from raw YAML text require a base directory.");
        }

        return LoadTemplateHierarchy(
            workflow,
            sourcePath: "<inline>",
            Path.GetFullPath(baseDirectory),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            parameterOverrides);
    }

    private WorkflowDefinition LoadFromFileInternal(
        string path,
        ISet<string> visited,
        IDictionary<string, object>? parameterOverrides)
    {
        if (!visited.Add(path))
        {
            throw new InvalidOperationException($"Template cycle detected while loading '{path}'.");
        }

        try
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Workflow file '{path}' was not found.", path);
            }

            var yaml = File.ReadAllText(path);
            var workflow = _parser.Parse(yaml);

            if (string.IsNullOrWhiteSpace(workflow.Template))
            {
                var clone = CloneWorkflow(workflow);
                clone.SourcePath = path;
                clone.StageSourcePath = path;
                StampLocalSources(clone, path);
                return ApplyParameterOverrides(clone, parameterOverrides);
            }

            return LoadTemplateHierarchy(
                workflow,
                path,
                Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory(),
                visited,
                parameterOverrides);
        }
        finally
        {
            visited.Remove(path);
        }
    }

    private WorkflowDefinition LoadTemplateHierarchy(
        WorkflowDefinition child,
        string sourcePath,
        string baseDirectory,
        ISet<string> visited,
        IDictionary<string, object>? parameterOverrides)
    {
        var templatePath = ResolveTemplatePath(baseDirectory, child.Template!);
        var parent = LoadFromFileInternal(templatePath, visited, null);
        var merged = MergeTemplate(parent, child, sourcePath, templatePath);
        return ApplyParameterOverrides(merged, parameterOverrides, sourcePath);
    }

    private static string ResolveTemplatePath(string baseDirectory, string templatePath)
    {
        var combined = Path.IsPathRooted(templatePath)
            ? templatePath
            : Path.Combine(baseDirectory, templatePath);
        return Path.GetFullPath(combined);
    }

    private static WorkflowDefinition MergeTemplate(
        WorkflowDefinition parent,
        WorkflowDefinition child,
        string childSourcePath,
        string templatePath)
    {
        if (child.Stages.Count > 0)
        {
            throw new InvalidOperationException(
                $"Workflow '{childSourcePath}' cannot define stages when using template '{templatePath}'. Override parameters, variables, or execution settings instead.");
        }

        if (child.ParameterDefinitions.Count > 0)
        {
            throw new InvalidOperationException(
                $"Workflow '{childSourcePath}' cannot define new parameter schemas when using template '{templatePath}'. Declare parameter values only.");
        }

        var merged = CloneWorkflow(parent);
        merged.SourcePath = childSourcePath;
        merged.StageSourcePath = parent.StageSourcePath ?? templatePath;
        if (!string.IsNullOrWhiteSpace(child.Name))
        {
            merged.Name = child.Name;
        }

        if (child.MaxParallelism is not null)
        {
            merged.MaxParallelism = child.MaxParallelism;
        }

        if (child.ContinueOnError is not null)
        {
            merged.ContinueOnError = child.ContinueOnError;
        }

        foreach (var (key, value) in child.ParameterValues)
        {
            merged.ParameterValues[key] = CloneValue(value);
            merged.ParameterValueSources[key] = childSourcePath;
        }

        foreach (var (key, value) in child.Variables)
        {
            merged.Variables[key] = CloneValue(value);
            merged.VariableSources[key] = childSourcePath;
        }

        return merged;
    }

    private static WorkflowDefinition ApplyParameterOverrides(WorkflowDefinition workflow, IDictionary<string, object>? parameterOverrides, string? sourcePath = null)
    {
        if (parameterOverrides is null)
        {
            return workflow;
        }

        foreach (var (key, value) in parameterOverrides)
        {
            workflow.ParameterValues[key] = CloneValue(value);
            if (!string.IsNullOrWhiteSpace(sourcePath))
            {
                workflow.ParameterValueSources[key] = sourcePath;
            }
        }

        return workflow;
    }

    private static WorkflowDefinition CloneWorkflow(WorkflowDefinition workflow)
    {
        var clone = new WorkflowDefinition
        {
            Name = workflow.Name,
            Version = workflow.Version,
            Template = workflow.Template,
            SourcePath = workflow.SourcePath,
            StageSourcePath = workflow.StageSourcePath,
            MaxParallelism = workflow.MaxParallelism,
            ContinueOnError = workflow.ContinueOnError
        };

        foreach (var (key, value) in workflow.ParameterDefinitions)
        {
            clone.ParameterDefinitions[key] = new ParameterDefinition
            {
                Type = value.Type,
                Required = value.Required,
                Default = value.Default is null ? null : CloneValue(value.Default),
                Description = value.Description,
                AllowedValues = value.AllowedValues.Select(CloneValue).ToList(),
                Minimum = value.Minimum,
                Maximum = value.Maximum,
                MinLength = value.MinLength,
                MaxLength = value.MaxLength,
                Pattern = value.Pattern,
                ItemType = value.ItemType,
                RequiredProperties = new List<string>(value.RequiredProperties)
            };
        }

        foreach (var (key, value) in workflow.ParameterValues)
        {
            clone.ParameterValues[key] = CloneValue(value);
        }

        foreach (var (key, value) in workflow.Variables)
        {
            clone.Variables[key] = CloneValue(value);
        }

        foreach (var (key, value) in workflow.ParameterDefinitionSources)
        {
            clone.ParameterDefinitionSources[key] = value;
        }

        foreach (var (key, value) in workflow.ParameterValueSources)
        {
            clone.ParameterValueSources[key] = value;
        }

        foreach (var (key, value) in workflow.VariableSources)
        {
            clone.VariableSources[key] = value;
        }

        foreach (var stage in workflow.Stages)
        {
            var stageClone = new StageDefinition
            {
                Stage = stage.Stage
            };

            foreach (var job in stage.Jobs)
            {
                var jobClone = new JobDefinition
                {
                    Job = job.Job,
                    MaxParallelism = job.MaxParallelism,
                    ContinueOnError = job.ContinueOnError
                };

                foreach (var step in job.Steps)
                {
                    var stepClone = new StepDefinition
                    {
                        Step = step.Step,
                        Type = step.Type,
                        Condition = step.Condition,
                        SourcePath = step.SourcePath,
                        TimeoutMs = step.TimeoutMs,
                        Retries = step.Retries,
                        ContinueOnError = step.ContinueOnError
                    };

                    foreach (var (key, value) in step.With)
                    {
                        stepClone.With[key] = CloneValue(value);
                    }

                    foreach (var dependency in step.DependsOn)
                    {
                        stepClone.DependsOn.Add(dependency);
                    }

                    jobClone.Steps.Add(stepClone);
                }

                stageClone.Jobs.Add(jobClone);
            }

            clone.Stages.Add(stageClone);
        }

        return clone;
    }

    private static void StampLocalSources(WorkflowDefinition workflow, string sourcePath)
    {
        workflow.SourcePath = sourcePath;
        workflow.StageSourcePath = sourcePath;

        foreach (var key in workflow.ParameterDefinitions.Keys)
        {
            workflow.ParameterDefinitionSources[key] = sourcePath;
        }

        foreach (var key in workflow.ParameterValues.Keys)
        {
            workflow.ParameterValueSources[key] = sourcePath;
        }

        foreach (var key in workflow.Variables.Keys)
        {
            workflow.VariableSources[key] = sourcePath;
        }

        foreach (var stage in workflow.Stages)
        {
            foreach (var job in stage.Jobs)
            {
                foreach (var step in job.Steps)
                {
                    step.SourcePath = sourcePath;
                }
            }
        }
    }
    private static object CloneValue(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is IDictionary<string, object> typedDictionary)
        {
            var clone = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, child) in typedDictionary)
            {
                clone[key] = CloneValue(child);
            }

            return clone;
        }

        if (value is System.Collections.IDictionary dictionary)
        {
            var clone = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                clone[entry.Key?.ToString() ?? string.Empty] = CloneValue(entry.Value);
            }

            return clone;
        }

        if (value is IEnumerable<object> typedEnumerable && value is not string)
        {
            return typedEnumerable.Select(CloneValue).ToList();
        }

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            var clone = new List<object>();
            foreach (var item in enumerable)
            {
                clone.Add(CloneValue(item));
            }

            return clone;
        }

        return value;
    }
}



