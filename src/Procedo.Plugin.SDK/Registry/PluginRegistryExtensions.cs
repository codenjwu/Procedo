using System.Reflection;
using System.Text.Json;

namespace Procedo.Plugin.SDK;

public static class PluginRegistryExtensions
{
    public static IPluginRegistry UseServiceProvider(this IPluginRegistry registry, IServiceProvider serviceProvider)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (serviceProvider is null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        if (registry is not PluginRegistry pluginRegistry)
        {
            throw new InvalidOperationException("The configured registry does not support service-provider-backed activation.");
        }

        pluginRegistry.ServiceProvider = serviceProvider;
        return registry;
    }

    public static void Register(this IPluginRegistry registry, string stepType, Func<StepContext, StepResult> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        registry.Register(stepType, context => Task.FromResult(handler(context)));
    }

    public static void Register(this IPluginRegistry registry, string stepType, Func<StepContext, Task<StepResult>> handler)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        registry.Register(stepType, () => new DelegateProcedoStep(handler));
    }

    public static bool TryRegister(this IPluginRegistry registry, string stepType, Func<StepContext, StepResult> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return registry.TryRegister(stepType, context => Task.FromResult(handler(context)));
    }

    public static bool TryRegister(this IPluginRegistry registry, string stepType, Func<StepContext, Task<StepResult>> handler)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return registry.TryRegister(stepType, () => new DelegateProcedoStep(handler));
    }

    public static void RegisterOrThrow(this IPluginRegistry registry, string stepType, Func<StepContext, StepResult> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        registry.RegisterOrThrow(stepType, context => Task.FromResult(handler(context)));
    }

    public static void RegisterOrThrow(this IPluginRegistry registry, string stepType, Func<StepContext, Task<StepResult>> handler)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        registry.RegisterOrThrow(stepType, () => new DelegateProcedoStep(handler));
    }

    public static void Register<TStep>(this IPluginRegistry registry, string stepType)
        where TStep : class, IProcedoStep
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        registry.Register(stepType, () => StepActivationUtilities.CreateStep<TStep>(GetServiceProvider(registry)));
    }

    public static bool TryRegister<TStep>(this IPluginRegistry registry, string stepType)
        where TStep : class, IProcedoStep
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        return registry.TryRegister(stepType, () => StepActivationUtilities.CreateStep<TStep>(GetServiceProvider(registry)));
    }

    public static void RegisterOrThrow<TStep>(this IPluginRegistry registry, string stepType)
        where TStep : class, IProcedoStep
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        registry.RegisterOrThrow(stepType, () => StepActivationUtilities.CreateStep<TStep>(GetServiceProvider(registry)));
    }

    public static void RegisterMethod(this IPluginRegistry registry, string stepType, Delegate method)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        registry.Register(stepType, () => new MethodBoundProcedoStep(method, GetServiceProvider(registry)));
    }

    public static bool TryRegisterMethod(this IPluginRegistry registry, string stepType, Delegate method)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        return registry.TryRegister(stepType, () => new MethodBoundProcedoStep(method, GetServiceProvider(registry)));
    }

    public static void RegisterMethodOrThrow(this IPluginRegistry registry, string stepType, Delegate method)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        registry.RegisterOrThrow(stepType, () => new MethodBoundProcedoStep(method, GetServiceProvider(registry)));
    }

    internal static IServiceProvider? GetServiceProvider(IPluginRegistry registry)
        => registry is PluginRegistry pluginRegistry ? pluginRegistry.ServiceProvider : null;
}

internal sealed class DelegateProcedoStep : IProcedoStep
{
    private readonly Func<StepContext, Task<StepResult>> _handler;

    public DelegateProcedoStep(Func<StepContext, Task<StepResult>> handler)
    {
        _handler = handler;
    }

    public Task<StepResult> ExecuteAsync(StepContext context) => _handler(context);
}

internal sealed class MethodBoundProcedoStep : IProcedoStep
{
    private readonly Delegate _method;
    private readonly IServiceProvider? _serviceProvider;

    public MethodBoundProcedoStep(Delegate method, IServiceProvider? serviceProvider)
    {
        _method = method;
        _serviceProvider = serviceProvider;
    }

    public async Task<StepResult> ExecuteAsync(StepContext context)
    {
        try
        {
            var arguments = StepBindingUtilities.BindParameters(_method.Method.GetParameters(), context, _serviceProvider);
            var invocationResult = _method.DynamicInvoke(arguments);
            var result = await StepBindingUtilities.UnwrapAsyncReturnAsync(invocationResult).ConfigureAwait(false);
            return StepBindingUtilities.ToStepResult(result);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw new InvalidOperationException(
                $"Failed to invoke bound method '{StepBindingUtilities.GetMethodDisplayName(_method.Method)}': {ex.InnerException.Message}",
                ex.InnerException);
        }
        catch (Exception ex) when (ex is not InvalidOperationException && ex is not TargetInvocationException)
        {
            throw new InvalidOperationException(
                $"Failed to invoke bound method '{StepBindingUtilities.GetMethodDisplayName(_method.Method)}': {ex.Message}",
                ex);
        }
    }
}

internal static class StepActivationUtilities
{
    public static TStep CreateStep<TStep>(IServiceProvider? serviceProvider)
        where TStep : class, IProcedoStep
        => (TStep)CreateStep(typeof(TStep), serviceProvider);

    public static IProcedoStep CreateStep(Type stepType, IServiceProvider? serviceProvider)
    {
        var constructors = stepType
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(static ctor => ctor.GetParameters().Length)
            .ToArray();

        foreach (var constructor in constructors)
        {
            if (TryCreate(constructor, serviceProvider, out var instance))
            {
                return (IProcedoStep)instance;
            }
        }

        throw new InvalidOperationException($"Unable to construct step type '{stepType.FullName}'. Ensure it has a parameterless constructor or all constructor dependencies are available from IServiceProvider.");
    }

    private static bool TryCreate(ConstructorInfo constructor, IServiceProvider? serviceProvider, out object instance)
    {
        var parameters = constructor.GetParameters();
        var arguments = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var service = serviceProvider?.GetService(parameter.ParameterType);
            if (service is not null)
            {
                arguments[i] = service;
                continue;
            }

            if (parameter.HasDefaultValue)
            {
                arguments[i] = parameter.DefaultValue;
                continue;
            }

            instance = null!;
            return false;
        }

        instance = constructor.Invoke(arguments);
        return true;
    }
}

internal static class StepBindingUtilities
{
    public static object?[] BindParameters(ParameterInfo[] parameters, StepContext context, IServiceProvider? serviceProvider)
    {
        var arguments = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            arguments[i] = ResolveParameter(parameters[i], context, serviceProvider);
        }

        return arguments;
    }

    public static string GetMethodDisplayName(MethodInfo method)
        => method.DeclaringType is null
            ? method.Name
            : $"{method.DeclaringType.FullName}.{method.Name}";

    public static async Task<object?> UnwrapAsyncReturnAsync(object? invocationResult)
    {
        if (invocationResult is null)
        {
            return null;
        }

        if (invocationResult is Task task)
        {
            await task.ConfigureAwait(false);
            var taskType = task.GetType();
            return taskType.IsGenericType ? taskType.GetProperty("Result")?.GetValue(task) : null;
        }

        var type = invocationResult.GetType();
        if (type == typeof(ValueTask))
        {
            await ((ValueTask)invocationResult).ConfigureAwait(false);
            return null;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var asTask = type.GetMethod("AsTask", BindingFlags.Public | BindingFlags.Instance);
            if (asTask?.Invoke(invocationResult, null) is Task valueTaskAsTask)
            {
                await valueTaskAsTask.ConfigureAwait(false);
                return valueTaskAsTask.GetType().GetProperty("Result")?.GetValue(valueTaskAsTask);
            }
        }

        return invocationResult;
    }

    public static StepResult ToStepResult(object? value)
    {
        if (value is StepResult stepResult)
        {
            return stepResult;
        }

        if (value is null)
        {
            return new StepResult { Success = true };
        }

        if (value is IDictionary<string, object> outputMap)
        {
            return new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>(outputMap, StringComparer.OrdinalIgnoreCase)
            };
        }

        if (value is System.Collections.IDictionary dictionary)
        {
            var dictionaryOutputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                if (entry.Key is not null)
                {
                    dictionaryOutputs[entry.Key.ToString() ?? string.Empty] = entry.Value ?? string.Empty;
                }
            }

            return new StepResult
            {
                Success = true,
                Outputs = dictionaryOutputs
            };
        }

        if (IsScalar(value.GetType()))
        {
            return new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["value"] = value
                }
            };
        }

        var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(static property => property.CanRead)
            .ToArray();

        if (properties.Length == 0)
        {
            return new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["value"] = value
                }
            };
        }

        var reflected = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in properties)
        {
            reflected[property.Name] = property.GetValue(value) ?? string.Empty;
        }

        return new StepResult
        {
            Success = true,
            Outputs = reflected
        };
    }

    private static object? ResolveParameter(ParameterInfo parameter, StepContext context, IServiceProvider? serviceProvider)
    {
        try
        {
            return ResolveParameterCore(parameter, context, serviceProvider);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw CreateBindingException(parameter, context, ex.Message, ex);
        }
    }

    private static object? ResolveParameterCore(ParameterInfo parameter, StepContext context, IServiceProvider? serviceProvider)
    {
        if (parameter.GetCustomAttribute<FromStepContextAttribute>() is not null)
        {
            if (!parameter.ParameterType.IsAssignableFrom(typeof(StepContext)) && parameter.ParameterType != typeof(StepContext))
            {
                throw CreateBindingException(parameter, context, "[FromStepContext] can only be used with StepContext parameters.", null);
            }

            return context;
        }

        if (parameter.GetCustomAttribute<FromCancellationTokenAttribute>() is not null)
        {
            if (parameter.ParameterType != typeof(CancellationToken))
            {
                throw CreateBindingException(parameter, context, "[FromCancellationToken] can only be used with CancellationToken parameters.", null);
            }

            return context.CancellationToken;
        }

        if (parameter.GetCustomAttribute<FromLoggerAttribute>() is not null)
        {
            if (parameter.ParameterType != typeof(ILogger))
            {
                throw CreateBindingException(parameter, context, "[FromLogger] can only be used with ILogger parameters.", null);
            }

            return context.Logger;
        }

        if (parameter.GetCustomAttribute<FromServicesAttribute>() is not null)
        {
            var explicitService = serviceProvider?.GetService(parameter.ParameterType);
            if (explicitService is null)
            {
                throw CreateBindingException(parameter, context, $"No service of type '{parameter.ParameterType.Name}' is available from IServiceProvider.", null);
            }

            return explicitService;
        }

        if (parameter.ParameterType == typeof(StepContext))
        {
            return context;
        }

        if (parameter.ParameterType == typeof(CancellationToken))
        {
            return context.CancellationToken;
        }

        if (parameter.ParameterType == typeof(ILogger))
        {
            return context.Logger;
        }

        if (parameter.ParameterType == typeof(IServiceProvider))
        {
            return serviceProvider;
        }

        if (parameter.ParameterType == typeof(IDictionary<string, object>))
        {
            return context.Inputs;
        }

        var service = serviceProvider?.GetService(parameter.ParameterType);
        if (service is not null)
        {
            return service;
        }

        var inputName = parameter.GetCustomAttribute<StepInputAttribute>()?.Name ?? parameter.Name ?? string.Empty;

        if (TryGetInputValue(context.Inputs, inputName, out var inputValue))
        {
            return ConvertValue(inputValue, parameter.ParameterType);
        }

        if (TryBindComplexObject(parameter.ParameterType, context.Inputs, inputName, out var complexValue))
        {
            return complexValue;
        }

        if (parameter.HasDefaultValue)
        {
            return parameter.DefaultValue;
        }

        throw CreateBindingException(parameter, context, $"No matching input or service was found for '{inputName}'.", null);
    }

    private static bool TryGetInputValue(IDictionary<string, object> inputs, string name, out object? value)
    {
        if (inputs.TryGetValue(name, out value))
        {
            return true;
        }

        foreach (var pair in inputs)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryBindComplexObject(Type targetType, IDictionary<string, object> inputs, string inputName, out object? value)
    {
        value = null;

        if (!IsComplexObjectType(targetType))
        {
            return false;
        }

        if (TryGetInputValue(inputs, inputName, out var nestedValue))
        {
            value = ConvertValue(nestedValue, targetType);
            return true;
        }

        if (inputs.Count == 0)
        {
            return false;
        }

        value = ConvertValue(inputs, targetType);
        return value is not null;
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null)
        {
            return null;
        }

        var nullableUnderlyingType = Nullable.GetUnderlyingType(targetType);
        if (nullableUnderlyingType is not null)
        {
            return ConvertValue(value, nullableUnderlyingType);
        }

        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (targetType == typeof(string))
        {
            return value.ToString();
        }

        if (targetType == typeof(Guid))
        {
            return Guid.Parse(value.ToString() ?? string.Empty);
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value.ToString() ?? string.Empty, ignoreCase: true);
        }

        if (targetType.IsArray)
        {
            var elementType = targetType.GetElementType()!;
            var sourceValues = value is System.Collections.IEnumerable enumerable && value is not string
                ? enumerable.Cast<object?>().ToArray()
                : new object?[] { value };
            var array = Array.CreateInstance(elementType, sourceValues.Length);
            for (var i = 0; i < sourceValues.Length; i++)
            {
                array.SetValue(ConvertValue(sourceValues[i], elementType), i);
            }

            return array;
        }

        if (targetType.IsGenericType)
        {
            var genericTypeDefinition = targetType.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof(List<>) || genericTypeDefinition == typeof(IEnumerable<>) || genericTypeDefinition == typeof(IReadOnlyList<>))
            {
                var elementType = targetType.GetGenericArguments()[0];
                var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
                var sourceValues = value is System.Collections.IEnumerable enumerable && value is not string
                    ? enumerable.Cast<object?>()
                    : new object?[] { value };
                foreach (var item in sourceValues)
                {
                    list.Add(ConvertValue(item, elementType));
                }

                return list;
            }
        }

        if (targetType == typeof(object))
        {
            return value;
        }

        if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(targetType))
        {
            return Convert.ChangeType(value, targetType);
        }

        var json = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize(json, targetType, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private static InvalidOperationException CreateBindingException(ParameterInfo parameter, StepContext context, string reason, Exception? innerException)
    {
        var inputName = parameter.GetCustomAttribute<StepInputAttribute>()?.Name ?? parameter.Name ?? string.Empty;
        var availableInputs = context.Inputs.Count == 0
            ? "<none>"
            : string.Join(", ", context.Inputs.Keys.OrderBy(static key => key, StringComparer.OrdinalIgnoreCase));

        return new InvalidOperationException(
            $"Unable to bind parameter '{parameter.Name}' (type '{parameter.ParameterType.Name}', input '{inputName}') for method '{GetMethodDisplayName((MethodInfo)parameter.Member)}'. {reason} Available inputs: {availableInputs}.",
            innerException);
    }

    private static bool IsScalar(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive
            || underlying.IsEnum
            || underlying == typeof(string)
            || underlying == typeof(decimal)
            || underlying == typeof(Guid)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(TimeSpan);
    }

    private static bool IsComplexObjectType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (IsScalar(underlying))
        {
            return false;
        }

        if (underlying == typeof(object) || typeof(System.Collections.IEnumerable).IsAssignableFrom(underlying))
        {
            return false;
        }

        return underlying.IsClass || (underlying.IsValueType && !underlying.IsPrimitive && !underlying.IsEnum);
    }
}
