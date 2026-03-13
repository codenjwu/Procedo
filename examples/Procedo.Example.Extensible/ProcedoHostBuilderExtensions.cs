using Procedo.Engine.Hosting;
using Procedo.Observability.Sinks;

internal static class ProcedoHostBuilderExtensions
{
    public static ProcedoHostBuilder UseStrictValidation(this ProcedoHostBuilder builder)
        => builder.ConfigureValidation(static validation => validation.TreatWarningsAsErrors = true);

    public static ProcedoHostBuilder UseConsoleEvents(this ProcedoHostBuilder builder)
        => builder.UseEventSink(new ConsoleExecutionEventSink());
}
