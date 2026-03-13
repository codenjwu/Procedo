using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Procedo.Core.Models;
using Procedo.Expressions;
using Procedo.Plugin.SDK;
using Procedo.Validation.Models;

namespace Procedo.Validation;

public sealed class ProcedoWorkflowValidator
{
    public ValidationResult Validate(
        WorkflowDefinition workflow,
        IPluginRegistry? pluginRegistry = null,
        ValidationOptions? options = null)
    {
        if (workflow is null)
        {
            throw new ArgumentNullException(nameof(workflow));
        }

        options ??= ValidationOptions.Permissive;
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(workflow.Name))
        {
            result.AddError("PV001", "Workflow name is required.", "name");
        }

        if (workflow.Version <= 0)
        {
            result.AddError("PV002", "Workflow version must be greater than zero.", "version");
        }

        ValidateParameters(workflow, result);
        ValidateWorkflowVariables(workflow, result);
        ValidateStages(workflow, result, pluginRegistry, options);
        PopulateIssueSourcePaths(workflow, result);
        return result;
    }

    private static void PopulateIssueSourcePaths(WorkflowDefinition workflow, ValidationResult result)
    {
        foreach (var issue in result.Issues)
        {
            issue.SourcePath ??= ResolveIssueSourcePath(workflow, issue);
        }
    }

    private static string? ResolveIssueSourcePath(WorkflowDefinition workflow, ValidationIssue issue)
    {
        if (issue.Path.StartsWith("stages[", StringComparison.OrdinalIgnoreCase))
        {
            return workflow.StageSourcePath ?? workflow.SourcePath;
        }

        if (issue.Path.Equals("variables", StringComparison.OrdinalIgnoreCase))
        {
            return workflow.VariableSources.Values.FirstOrDefault()
                ?? workflow.SourcePath
                ?? workflow.StageSourcePath;
        }

        if (issue.Path.StartsWith("variables.", StringComparison.OrdinalIgnoreCase))
        {
            var variableName = issue.Path.Substring("variables.".Length).Split('.', StringSplitOptions.RemoveEmptyEntries)[0];
            if (workflow.VariableSources.TryGetValue(variableName, out var variableSource))
            {
                return variableSource;
            }

            return workflow.SourcePath ?? workflow.StageSourcePath;
        }

        if (issue.Path.StartsWith("parameters", StringComparison.OrdinalIgnoreCase))
        {
            var parameterName = TryGetParameterName(issue.Path);
            if (!string.IsNullOrWhiteSpace(parameterName))
            {
                if (issue.Code is "PV013" or "PV014")
                {
                    if (workflow.ParameterValueSources.TryGetValue(parameterName, out var valueSource))
                    {
                        return valueSource;
                    }
                }
                else
                {
                    if (workflow.ParameterDefinitionSources.TryGetValue(parameterName, out var definitionSource))
                    {
                        return definitionSource;
                    }

                    if (workflow.ParameterValueSources.TryGetValue(parameterName, out var valueSource))
                    {
                        return valueSource;
                    }
                }
            }

            return workflow.SourcePath ?? workflow.StageSourcePath;
        }

        return workflow.SourcePath ?? workflow.StageSourcePath;
    }

    private static string? TryGetParameterName(string path)
    {
        if (!path.StartsWith("parameters.", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var remainder = path.Substring("parameters.".Length);
        if (string.IsNullOrWhiteSpace(remainder))
        {
            return null;
        }

        var separatorIndex = remainder.IndexOf('.');
        return separatorIndex >= 0 ? remainder[..separatorIndex] : remainder;
    }
    private static void ValidateParameters(WorkflowDefinition workflow, ValidationResult result)
    {
        foreach (var (name, definition) in workflow.ParameterDefinitions)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                result.AddError("PV010", "Parameter name is required.", "parameters");
                continue;
            }

            if (definition.Required && definition.Default is null && !workflow.ParameterValues.ContainsKey(name))
            {
                result.AddError("PV011", $"Required parameter '{name}' does not have a supplied value.", $"parameters.{name}");
            }

            ValidateParameterDefinitionConstraints(name, definition, result);

            if (definition.Default is not null && !WorkflowContextResolver.IsCompatibleParameterValue(definition, definition.Default))
            {
                result.AddError("PV012", $"Default value for parameter '{name}' is not compatible with declared type '{definition.Type}'.", $"parameters.{name}.default");
            }
        }

        foreach (var (name, value) in workflow.ParameterValues)
        {
            if (workflow.ParameterDefinitions.Count > 0 && !workflow.ParameterDefinitions.ContainsKey(name))
            {
                result.AddError("PV013", $"Parameter '{name}' is supplied but not declared in the workflow parameter schema.", $"parameters.{name}");
                continue;
            }

            if (workflow.ParameterDefinitions.TryGetValue(name, out var definition)
                && !WorkflowContextResolver.IsCompatibleParameterValue(definition, value))
            {
                result.AddError("PV014", $"Parameter '{name}' value is not compatible with declared type '{definition.Type}'.", $"parameters.{name}");
            }
        }
    }

    private static void ValidateParameterDefinitionConstraints(string name, ParameterDefinition definition, ValidationResult result)
    {
        var type = NormalizeParameterType(definition.Type);

        if (definition.Minimum is not null || definition.Maximum is not null)
        {
            if (!IsNumericParameterType(type))
            {
                result.AddError("PV021", $"Parameter '{name}' uses min/max constraints but is not a numeric type.", $"parameters.{name}");
            }

            if (definition.Minimum is not null && definition.Maximum is not null && definition.Minimum > definition.Maximum)
            {
                result.AddError("PV022", $"Parameter '{name}' has a min value greater than its max value.", $"parameters.{name}");
            }
        }

        if (definition.MinLength is not null || definition.MaxLength is not null || !string.IsNullOrWhiteSpace(definition.Pattern))
        {
            if (!string.Equals(type, "string", StringComparison.OrdinalIgnoreCase))
            {
                result.AddError("PV023", $"Parameter '{name}' uses string constraints but is not declared as type 'string'.", $"parameters.{name}");
            }

            if (definition.MinLength is not null && definition.MaxLength is not null && definition.MinLength > definition.MaxLength)
            {
                result.AddError("PV024", $"Parameter '{name}' has a min_length greater than its max_length.", $"parameters.{name}");
            }

            if (!string.IsNullOrWhiteSpace(definition.Pattern))
            {
                try
                {
                    _ = Regex.IsMatch(string.Empty, definition.Pattern);
                }
                catch (ArgumentException)
                {
                    result.AddError("PV025", $"Parameter '{name}' has an invalid regular expression pattern.", $"parameters.{name}.pattern");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(definition.ItemType) && !IsArrayParameterType(type))
        {
            result.AddError("PV026", $"Parameter '{name}' declares item_type but is not an array type.", $"parameters.{name}.item_type");
        }

        if (definition.RequiredProperties.Count > 0 && !string.Equals(type, "object", StringComparison.OrdinalIgnoreCase))
        {
            result.AddError("PV027", $"Parameter '{name}' declares required_properties but is not an object type.", $"parameters.{name}.required_properties");
        }
    }

    private static void ValidateWorkflowVariables(WorkflowDefinition workflow, ValidationResult result)
    {
        var parameterNames = GetParameterNames(workflow);
        var variableNames = new HashSet<string>(workflow.Variables.Keys, StringComparer.OrdinalIgnoreCase);
        var dependencies = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, value) in workflow.Variables)
        {
            var path = $"variables.{name}";
            dependencies[name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var token in EnumerateExpressionTokens(value))
            {
                if (token.StartsWith("params.", StringComparison.OrdinalIgnoreCase))
                {
                    var parameterName = token.Substring("params.".Length);
                    if (!parameterNames.Contains(parameterName))
                    {
                        result.AddError("PV015", $"Variable '{name}' references unknown parameter '{parameterName}'.", path);
                    }

                    continue;
                }

                if (token.StartsWith("vars.", StringComparison.OrdinalIgnoreCase))
                {
                    var variableName = token.Substring("vars.".Length);
                    if (!variableNames.Contains(variableName))
                    {
                        result.AddError("PV016", $"Variable '{name}' references unknown variable '{variableName}'.", path);
                        continue;
                    }

                    if (string.Equals(variableName, name, StringComparison.OrdinalIgnoreCase))
                    {
                        result.AddError("PV017", $"Variable '{name}' cannot reference itself.", path);
                        continue;
                    }

                    dependencies[name].Add(variableName);
                    continue;
                }

                if (token.StartsWith("steps.", StringComparison.OrdinalIgnoreCase) || TryGetReferencedStep(token, out _))
                {
                    result.AddError("PV018", $"Workflow variable '{name}' cannot reference step outputs. Use params.* or vars.* only.", path);
                    continue;
                }

                result.AddError("PV019", $"Unsupported variable expression token '{token}'. Use params.<name> or vars.<name>.", path);
            }
        }

        if (HasVariableCycle(dependencies))
        {
            result.AddError("PV020", "Cyclic workflow variable references were detected.", "variables");
        }
    }

    private static void ValidateStages(
        WorkflowDefinition workflow,
        ValidationResult result,
        IPluginRegistry? pluginRegistry,
        ValidationOptions options)
    {
        var stageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var s = 0; s < workflow.Stages.Count; s++)
        {
            var stage = workflow.Stages[s];
            var stagePath = $"stages[{s}]";

            if (string.IsNullOrWhiteSpace(stage.Stage))
            {
                result.AddError("PV100", "Stage name is required.", $"{stagePath}.stage");
            }
            else if (!stageNames.Add(stage.Stage))
            {
                result.AddError("PV101", $"Duplicate stage name '{stage.Stage}'.", $"{stagePath}.stage");
            }

            ValidateJobs(workflow, stage, stagePath, result, pluginRegistry, options);
        }
    }

    private static void ValidateJobs(
        WorkflowDefinition workflow,
        StageDefinition stage,
        string stagePath,
        ValidationResult result,
        IPluginRegistry? pluginRegistry,
        ValidationOptions options)
    {
        var jobNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var j = 0; j < stage.Jobs.Count; j++)
        {
            var job = stage.Jobs[j];
            var jobPath = $"{stagePath}.jobs[{j}]";

            if (string.IsNullOrWhiteSpace(job.Job))
            {
                result.AddError("PV200", "Job name is required.", $"{jobPath}.job");
            }
            else if (!jobNames.Add(job.Job))
            {
                result.AddError("PV201", $"Duplicate job name '{job.Job}' in stage '{stage.Stage}'.", $"{jobPath}.job");
            }

            ValidateSteps(workflow, stage, job, jobPath, result, pluginRegistry, options);
        }
    }

    private static void ValidateSteps(
        WorkflowDefinition workflow,
        StageDefinition stage,
        JobDefinition job,
        string jobPath,
        ValidationResult result,
        IPluginRegistry? pluginRegistry,
        ValidationOptions options)
    {
        var stepNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stepLookup = new Dictionary<string, StepDefinition>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < job.Steps.Count; i++)
        {
            var step = job.Steps[i];
            var stepPath = $"{jobPath}.steps[{i}]";

            if (string.IsNullOrWhiteSpace(step.Step))
            {
                result.AddError("PV300", "Step id is required.", $"{stepPath}.step");
            }
            else if (!stepNames.Add(step.Step))
            {
                result.AddError("PV301", $"Duplicate step id '{step.Step}' in job '{job.Job}'.", $"{stepPath}.step");
            }
            else
            {
                stepLookup[step.Step] = step;
            }

            if (string.IsNullOrWhiteSpace(step.Type))
            {
                result.AddError("PV302", "Step type is required.", $"{stepPath}.type");
            }
            else
            {
                if (!IsValidStepType(step.Type))
                {
                    result.AddError("PV303", $"Step type '{step.Type}' must use namespace.operation format.", $"{stepPath}.type");
                }

                if (pluginRegistry is not null && !pluginRegistry.TryResolve(step.Type, out _))
                {
                    result.AddError("PV304", $"No plugin registered for step type '{step.Type}'.", $"{stepPath}.type");
                }
            }
        }

        ValidateDependencies(stage, job, jobPath, stepLookup, result, options);
        ValidateInputExpressions(workflow, job, jobPath, stepLookup, result);
        ValidateCycles(stage, job, stepLookup, result);
    }

    private static void ValidateDependencies(
        StageDefinition stage,
        JobDefinition job,
        string jobPath,
        IReadOnlyDictionary<string, StepDefinition> stepLookup,
        ValidationResult result,
        ValidationOptions options)
    {
        for (var i = 0; i < job.Steps.Count; i++)
        {
            var step = job.Steps[i];
            var dependsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var d = 0; d < step.DependsOn.Count; d++)
            {
                var dep = step.DependsOn[d];
                var depPath = $"{jobPath}.steps[{i}].depends_on[{d}]";

                if (string.IsNullOrWhiteSpace(dep))
                {
                    result.AddError("PV305", "depends_on contains an empty dependency.", depPath);
                    continue;
                }

                if (string.Equals(dep, step.Step, StringComparison.OrdinalIgnoreCase))
                {
                    result.AddError("PV306", $"Step '{step.Step}' cannot depend on itself.", depPath);
                }

                if (!stepLookup.ContainsKey(dep))
                {
                    result.AddError("PV307", $"Step '{step.Step}' depends on unknown step '{dep}' in stage '{stage.Stage}', job '{job.Job}'.", depPath);
                }

                if (!dependsSet.Add(dep))
                {
                    AddConfigurableIssue(result, options, "PV308", $"Duplicate dependency '{dep}' found in step '{step.Step}'.", depPath);
                }
            }
        }
    }

    private static void ValidateInputExpressions(
        WorkflowDefinition workflow,
        JobDefinition job,
        string jobPath,
        IReadOnlyDictionary<string, StepDefinition> stepLookup,
        ValidationResult result)
    {
        var parameterNames = GetParameterNames(workflow);
        var variableNames = new HashSet<string>(workflow.Variables.Keys, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < job.Steps.Count; i++)
        {
            var step = job.Steps[i];
            var stepPath = $"{jobPath}.steps[{i}]";

            if (!string.IsNullOrWhiteSpace(step.Condition))
            {
                ValidateExpressionTokens(
                    step.Condition,
                    $"{stepPath}.condition",
                    step,
                    parameterNames,
                    variableNames,
                    stepLookup,
                    result,
                    allowLegacyStepTokens: false,
                    treatAsStandaloneExpression: true);
            }

            foreach (var (inputKey, inputValue) in step.With)
            {
                ValidateExpressionTokens(
                    inputValue,
                    $"{stepPath}.with.{inputKey}",
                    step,
                    parameterNames,
                    variableNames,
                    stepLookup,
                    result,
                    allowLegacyStepTokens: true,
                    treatAsStandaloneExpression: false);
            }
        }
    }

    private static void ValidateExpressionTokens(
        object expressionValue,
        string path,
        StepDefinition step,
        IReadOnlySet<string> parameterNames,
        IReadOnlySet<string> variableNames,
        IReadOnlyDictionary<string, StepDefinition> stepLookup,
        ValidationResult result,
        bool allowLegacyStepTokens,
        bool treatAsStandaloneExpression)
    {
        foreach (var token in EnumerateExpressionTokens(expressionValue, treatAsStandaloneExpression))
        {
            if (token.StartsWith("params.", StringComparison.OrdinalIgnoreCase))
            {
                var parameterName = token.Substring("params.".Length);
                if (!parameterNames.Contains(parameterName))
                {
                    result.AddError("PV314", $"Expression references unknown parameter '{parameterName}'.", path);
                }

                continue;
            }

            if (token.StartsWith("vars.", StringComparison.OrdinalIgnoreCase))
            {
                var variableName = token.Substring("vars.".Length);
                if (!variableNames.Contains(variableName))
                {
                    result.AddError("PV315", $"Expression references unknown variable '{variableName}'.", path);
                }

                continue;
            }

            if (!TryGetReferencedStep(token, out var referencedStep) || (!allowLegacyStepTokens && !token.StartsWith("steps.", StringComparison.OrdinalIgnoreCase)))
            {
                result.AddError("PV310", $"Unsupported expression token '{token}'. Use params.<name>, vars.<name>, or steps.<step>.outputs.<key>.", path);
                continue;
            }

            if (!stepLookup.ContainsKey(referencedStep))
            {
                result.AddError("PV311", $"Expression references unknown step '{referencedStep}'.", path);
                continue;
            }

            if (string.Equals(referencedStep, step.Step, StringComparison.OrdinalIgnoreCase))
            {
                result.AddError("PV312", $"Step '{step.Step}' cannot reference its own outputs in expressions.", path);
                continue;
            }

            if (!DependsTransitivelyOn(step.Step, referencedStep, stepLookup, new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
            {
                result.AddError("PV313", $"Step '{step.Step}' references outputs from '{referencedStep}' but does not depend on it.", path);
            }
        }
    }

    private static IEnumerable<string> EnumerateExpressionTokens(object value, bool treatAsStandaloneExpression = false)
    {
        switch (value)
        {
            case string s:
                if (treatAsStandaloneExpression)
                {
                    IReadOnlyCollection<string>? references = null;
                    var parseFailed = false;
                    try
                    {
                        references = ExpressionResolver.ExtractReferencedTokensFromExpression(s);
                    }
                    catch (ExpressionResolutionException)
                    {
                        parseFailed = true;
                    }

                    if (parseFailed)
                    {
                        yield return s;
                        yield break;
                    }

                    foreach (var token in references!)
                    {
                        yield return token;
                    }

                    yield break;
                }

                foreach (var expression in ExpressionResolver.ExtractExpressions(s))
                {
                    IReadOnlyCollection<string>? references = null;
                    var parseFailed = false;
                    try
                    {
                        references = ExpressionResolver.ExtractReferencedTokensFromExpression(expression);
                    }
                    catch (ExpressionResolutionException)
                    {
                        parseFailed = true;
                    }

                    if (parseFailed)
                    {
                        yield return expression;
                        continue;
                    }

                    foreach (var token in references!)
                    {
                        yield return token;
                    }
                }
                break;

            case IDictionary dictionary:
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Value is null)
                    {
                        continue;
                    }

                    foreach (var token in EnumerateExpressionTokens(entry.Value))
                    {
                        yield return token;
                    }
                }
                break;

            case IEnumerable enumerable when value is not string:
                foreach (var item in enumerable)
                {
                    if (item is null)
                    {
                        continue;
                    }

                    foreach (var token in EnumerateExpressionTokens(item))
                    {
                        yield return token;
                    }
                }
                break;
        }
    }

    private static HashSet<string> GetParameterNames(WorkflowDefinition workflow)
    {
        var names = new HashSet<string>(workflow.ParameterDefinitions.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (var name in workflow.ParameterValues.Keys)
        {
            names.Add(name);
        }

        return names;
    }

    private static bool TryGetReferencedStep(string token, out string stepId)
    {
        stepId = string.Empty;

        if (token.StartsWith("steps.", StringComparison.OrdinalIgnoreCase))
        {
            var parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 4 && string.Equals(parts[2], "outputs", StringComparison.OrdinalIgnoreCase))
            {
                stepId = parts[1];
                return !string.IsNullOrWhiteSpace(stepId);
            }

            return false;
        }

        var legacy = token.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (legacy.Length == 2)
        {
            stepId = legacy[0];
            return !string.IsNullOrWhiteSpace(stepId);
        }

        return false;
    }

    private static bool DependsTransitivelyOn(
        string stepId,
        string dependencyCandidate,
        IReadOnlyDictionary<string, StepDefinition> stepLookup,
        ISet<string> visiting)
    {
        if (!stepLookup.TryGetValue(stepId, out var step))
        {
            return false;
        }

        if (!visiting.Add(stepId))
        {
            return false;
        }

        try
        {
            foreach (var dep in step.DependsOn)
            {
                if (string.Equals(dep, dependencyCandidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!stepLookup.ContainsKey(dep))
                {
                    continue;
                }

                if (DependsTransitivelyOn(dep, dependencyCandidate, stepLookup, visiting))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            visiting.Remove(stepId);
        }
    }

    private static bool HasVariableCycle(IReadOnlyDictionary<string, HashSet<string>> dependencies)
    {
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in dependencies.Keys)
        {
            if (HasVariableCycle(name, dependencies, visiting, visited))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasVariableCycle(
        string name,
        IReadOnlyDictionary<string, HashSet<string>> dependencies,
        ISet<string> visiting,
        ISet<string> visited)
    {
        if (visited.Contains(name))
        {
            return false;
        }

        if (!visiting.Add(name))
        {
            return true;
        }

        if (dependencies.TryGetValue(name, out var deps))
        {
            foreach (var dep in deps)
            {
                if (HasVariableCycle(dep, dependencies, visiting, visited))
                {
                    return true;
                }
            }
        }

        visiting.Remove(name);
        visited.Add(name);
        return false;
    }

    private static void AddConfigurableIssue(
        ValidationResult result,
        ValidationOptions options,
        string code,
        string message,
        string path)
    {
        if (options.TreatWarningsAsErrors)
        {
            result.AddError(code, message, path);
            return;
        }

        result.AddWarning(code, message, path);
    }

    private static void ValidateCycles(
        StageDefinition stage,
        JobDefinition job,
        IReadOnlyDictionary<string, StepDefinition> stepLookup,
        ValidationResult result)
    {
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var stepId in stepLookup.Keys)
        {
            if (visited.Contains(stepId))
            {
                continue;
            }

            if (HasCycle(stepId, stepLookup, visiting, visited))
            {
                result.AddError("PV309", $"Cyclic dependency detected in stage '{stage.Stage}', job '{job.Job}'.", $"stages[{stage.Stage}].jobs[{job.Job}].steps");
                return;
            }
        }
    }

    private static bool HasCycle(
        string stepId,
        IReadOnlyDictionary<string, StepDefinition> stepLookup,
        ISet<string> visiting,
        ISet<string> visited)
    {
        if (visiting.Contains(stepId))
        {
            return true;
        }

        if (visited.Contains(stepId))
        {
            return false;
        }

        visiting.Add(stepId);

        var step = stepLookup[stepId];
        foreach (var dependency in step.DependsOn)
        {
            if (!stepLookup.ContainsKey(dependency))
            {
                continue;
            }

            if (HasCycle(dependency, stepLookup, visiting, visited))
            {
                return true;
            }
        }

        visiting.Remove(stepId);
        visited.Add(stepId);
        return false;
    }

    private static bool IsValidStepType(string type)
    {
        var parts = type.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        return IsSegmentValid(parts[0]) && IsSegmentValid(parts[1]);
    }

    private static bool IsSegmentValid(string segment)
    {
        foreach (var c in segment)
        {
            if (char.IsLetterOrDigit(c) || c is '_' or '-')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool IsNumericParameterType(string type)
        => type is "int" or "integer" or "number" or "double" or "float" or "decimal";

    private static bool IsArrayParameterType(string type)
        => type is "array" or "list";

    private static string NormalizeParameterType(string? type)
        => string.IsNullOrWhiteSpace(type) ? "string" : type.Trim().ToLowerInvariant();
}



