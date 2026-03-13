using Procedo.Expressions;

namespace Procedo.UnitTests;

public class ExpressionResolverTests
{
    [Fact]
    public void ResolveInputs_Should_Resolve_Direct_And_Interpolated_Expressions()
    {
        var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["download.path"] = "/tmp/data.json",
            ["steps.download.outputs.path"] = "/tmp/data.json",
            ["vars.env"] = "prod",
            ["vars.retries"] = 3
        };

        var inputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["path"] = "${steps.download.outputs.path}",
            ["message"] = "env=${vars.env}, file=${download.path}",
            ["attempts"] = "${vars.retries}"
        };

        var resolved = ExpressionResolver.ResolveInputs(inputs, variables);

        Assert.Equal("/tmp/data.json", resolved["path"]);
        Assert.Equal("env=prod, file=/tmp/data.json", resolved["message"]);
        Assert.Equal(3, resolved["attempts"]);
    }

    [Fact]
    public void ResolveInputs_Should_Resolve_Nested_Dictionaries_And_Arrays()
    {
        var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["steps.a.outputs.value"] = 10,
            ["a.value"] = 10,
            ["vars.env"] = "dev"
        };

        var inputs = new Dictionary<string, object>
        {
            ["payload"] = new Dictionary<string, object>
            {
                ["arr"] = new List<object>
                {
                    "${steps.a.outputs.value}",
                    "prefix-${vars.env}",
                    1
                }
            }
        };

        var resolved = ExpressionResolver.ResolveInputs(inputs, variables);

        var payload = Assert.IsType<Dictionary<string, object>>(resolved["payload"]);
        var arr = Assert.IsType<List<object>>(payload["arr"]);

        Assert.Equal(10, arr[0]);
        Assert.Equal("prefix-dev", arr[1]);
        Assert.Equal(1, arr[2]);
    }

    [Fact]
    public void ResolveInputs_Should_Resolve_Parameter_Expressions()
    {
        var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["params.environment"] = "prod",
            ["params.region"] = "eastus",
            ["environment"] = "prod",
            ["region"] = "eastus"
        };

        var inputs = new Dictionary<string, object>
        {
            ["message"] = "Deploy ${params.environment} to ${params.region}"
        };

        var resolved = ExpressionResolver.ResolveInputs(inputs, variables);

        Assert.Equal("Deploy prod to eastus", resolved["message"]);
    }
    [Fact]
    public void ResolveInputs_Should_Throw_When_Token_Cannot_Be_Resolved()
    {
        var variables = new Dictionary<string, object>();
        var inputs = new Dictionary<string, object>
        {
            ["x"] = "${steps.unknown.outputs.value}"
        };

        Assert.Throws<ExpressionResolutionException>(() => ExpressionResolver.ResolveInputs(inputs, variables));
    }

    [Fact]
    public void ResolveInputs_Should_Evaluate_Function_Expressions()
    {
        var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["params.environment"] = "prod",
            ["params.region"] = "westus",
            ["vars.service"] = "orders-api",
            ["vars.retry_count"] = 3
        };

        var inputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["isProd"] = "${eq(params.environment, 'prod')}",
            ["endpoint"] = "${format('{0}-{1}', vars.service, params.region)}",
            ["matches"] = "${and(eq(params.environment, 'prod'), startsWith(vars.service, 'orders'))}",
            ["contains"] = "${contains(vars.service, 'api')}",
            ["membership"] = "${in(params.region, 'eastus', 'westus')}"
        };

        var resolved = ExpressionResolver.ResolveInputs(inputs, variables);

        Assert.Equal(true, resolved["isProd"]);
        Assert.Equal("orders-api-westus", resolved["endpoint"]);
        Assert.Equal(true, resolved["matches"]);
        Assert.Equal(true, resolved["contains"]);
        Assert.Equal(true, resolved["membership"]);
    }

    [Fact]
    public void EvaluateCondition_Should_Support_Or_Not_EndsWith_And_ArrayMembership()
    {
        var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["params.environment"] = "qa",
            ["params.region"] = "westus",
            ["params.allowed_regions"] = new List<object> { "eastus", "westus" },
            ["params.service_name"] = "orders-api"
        };

        Assert.True(ExpressionResolver.EvaluateCondition(
            "or(eq(params.environment, 'prod'), eq(params.environment, 'qa'))",
            variables));
        Assert.True(ExpressionResolver.EvaluateCondition(
            "not(startsWith(params.service_name, 'legacy-'))",
            variables));
        Assert.True(ExpressionResolver.EvaluateCondition(
            "endsWith(params.service_name, '-api')",
            variables));
        Assert.True(ExpressionResolver.EvaluateCondition(
            "in(params.region, params.allowed_regions)",
            variables));
    }

    [Fact]
    public void ExtractReferencedTokensFromExpression_Should_Return_Identifiers_Only()
    {
        var tokens = ExpressionResolver.ExtractReferencedTokensFromExpression(
            "and(eq(params.environment, 'prod'), startsWith(vars.service, 'orders'))");

        Assert.Equal(
            new[] { "params.environment", "vars.service" },
            tokens.ToArray());
    }
}



