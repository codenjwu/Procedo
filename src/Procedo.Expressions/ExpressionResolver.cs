using System.Collections;
using System.Globalization;
using System.Text.Json;

namespace Procedo.Expressions;

public static class ExpressionResolver
{
    public static Dictionary<string, object> ResolveInputs(
        IDictionary<string, object> inputs,
        IDictionary<string, object> variables)
    {
        var resolved = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in inputs)
        {
            resolved[key] = ResolveValue(value, variables);
        }

        return resolved;
    }

    public static object ResolveValue(object? value, IDictionary<string, object> variables)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is string text)
        {
            return ResolveString(text, variables);
        }

        if (value is IDictionary dictionary)
        {
            var mapped = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in dictionary)
            {
                var key = entry.Key?.ToString() ?? string.Empty;
                mapped[key] = ResolveValue(entry.Value, variables);
            }

            return mapped;
        }

        if (value is IEnumerable enumerable and not string)
        {
            var list = new List<object>();
            foreach (var item in enumerable)
            {
                list.Add(ResolveValue(item, variables));
            }

            return list;
        }

        return value;
    }

    public static IEnumerable<string> ExtractExpressions(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        return FindExpressions(text)
            .Select(static match => match.Expression)
            .Where(static v => !string.IsNullOrWhiteSpace(v))
            .ToArray();
    }

    public static object EvaluateExpression(string expression, IDictionary<string, object> variables)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ExpressionResolutionException("Expression is empty.");
        }

        var parser = new ExpressionParser(NormalizeStandaloneExpression(expression));
        var node = parser.Parse();
        return node.Evaluate(variables);
    }

    public static bool EvaluateCondition(string expression, IDictionary<string, object> variables)
    {
        var result = EvaluateExpression(expression, variables);
        return result switch
        {
            bool boolean => boolean,
            _ => throw new ExpressionResolutionException($"Condition expression '{expression}' must evaluate to a boolean.")
        };
    }

    public static IReadOnlyCollection<string> ExtractReferencedTokensFromExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return Array.Empty<string>();
        }

        var parser = new ExpressionParser(NormalizeStandaloneExpression(expression));
        var node = parser.Parse();
        return node.GetReferencedTokens().ToArray();
    }

    private static object ResolveString(string text, IDictionary<string, object> variables)
    {
        var matches = FindExpressions(text);
        if (matches.Count == 0)
        {
            return text;
        }

        if (matches.Count == 1 && matches[0].Index == 0 && matches[0].Length == text.Length)
        {
            return EvaluateExpression(matches[0].Expression, variables);
        }

        var builder = new System.Text.StringBuilder();
        var currentIndex = 0;
        foreach (var match in matches)
        {
            builder.Append(text, currentIndex, match.Index - currentIndex);
            var resolved = EvaluateExpression(match.Expression, variables);
            builder.Append(resolved?.ToString() ?? string.Empty);
            currentIndex = match.Index + match.Length;
        }
        builder.Append(text, currentIndex, text.Length - currentIndex);

        return builder.ToString();
    }

    private static List<ExpressionMatch> FindExpressions(string text)
    {
        var matches = new List<ExpressionMatch>();
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] != '$' || index + 1 >= text.Length || text[index + 1] != '{')
            {
                continue;
            }

            var start = index;
            index += 2;
            var quote = '\0';

            while (index < text.Length)
            {
                var current = text[index];
                if (quote == '\0' && (current == '\'' || current == '"'))
                {
                    quote = current;
                    index++;
                    continue;
                }

                if (quote != '\0' && current == quote)
                {
                    quote = '\0';
                    index++;
                    continue;
                }

                if (quote == '\0' && current == '}')
                {
                    var expression = text[(start + 2)..index];
                    matches.Add(new ExpressionMatch(start, index - start + 1, expression));
                    break;
                }

                index++;
            }
        }

        return matches;
    }

    private static string NormalizeStandaloneExpression(string expression)
    {
        var trimmed = expression.Trim();
        if (trimmed.StartsWith("${", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
        {
            return trimmed[2..^1].Trim();
        }

        return trimmed;
    }

    private static object ResolveIdentifier(string token, IDictionary<string, object> variables)
    {
        if (variables.TryGetValue(token, out var direct))
        {
            return direct;
        }

        if (token.StartsWith("steps.", StringComparison.OrdinalIgnoreCase))
        {
            var parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 4 && string.Equals(parts[2], "outputs", StringComparison.OrdinalIgnoreCase))
            {
                var legacyKey = $"{parts[1]}.{parts[3]}";
                if (variables.TryGetValue(legacyKey, out var legacyValue))
                {
                    return legacyValue;
                }
            }
        }

        if (token.StartsWith("vars.", StringComparison.OrdinalIgnoreCase))
        {
            if (variables.TryGetValue(token, out var varValue))
            {
                return varValue;
            }

            var varKey = token.Substring("vars.".Length);
            if (variables.TryGetValue(varKey, out var shortVar))
            {
                return shortVar;
            }
        }

        if (token.StartsWith("params.", StringComparison.OrdinalIgnoreCase))
        {
            if (variables.TryGetValue(token, out var parameterValue))
            {
                return parameterValue;
            }

            var parameterKey = token.Substring("params.".Length);
            if (variables.TryGetValue(parameterKey, out var shortParameter))
            {
                return shortParameter;
            }
        }

        throw new ExpressionResolutionException($"Unable to resolve expression token '{token}'.");
    }

    private static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool boolean => boolean,
            string text => !string.IsNullOrEmpty(text),
            sbyte number => number != 0,
            byte number => number != 0,
            short number => number != 0,
            ushort number => number != 0,
            int number => number != 0,
            uint number => number != 0,
            long number => number != 0,
            ulong number => number != 0,
            float number => Math.Abs(number) > float.Epsilon,
            double number => Math.Abs(number) > double.Epsilon,
            decimal number => number != 0,
            IEnumerable enumerable when value is not string => enumerable.Cast<object?>().Any(),
            _ => true
        };
    }

    private static bool AreEquivalentValues(object? left, object? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        if (left is string leftString && right is string rightString)
        {
            return string.Equals(leftString, rightString, StringComparison.OrdinalIgnoreCase);
        }

        if (TryConvertToDouble(left, out var leftNumber) && TryConvertToDouble(right, out var rightNumber))
        {
            return Math.Abs(leftNumber - rightNumber) < 0.0000001d;
        }

        if (Equals(left, right))
        {
            return true;
        }

        return JsonSerializer.Serialize(left) == JsonSerializer.Serialize(right);
    }

    private static bool TryConvertToDouble(object value, out double number)
    {
        switch (value)
        {
            case sbyte signedByte:
                number = signedByte;
                return true;
            case byte unsignedByte:
                number = unsignedByte;
                return true;
            case short shortValue:
                number = shortValue;
                return true;
            case ushort unsignedShort:
                number = unsignedShort;
                return true;
            case int intValue:
                number = intValue;
                return true;
            case uint unsignedInt:
                number = unsignedInt;
                return true;
            case long longValue:
                number = longValue;
                return true;
            case ulong unsignedLong:
                number = unsignedLong;
                return true;
            case float floatValue:
                number = floatValue;
                return true;
            case double doubleValue:
                number = doubleValue;
                return true;
            case decimal decimalValue:
                number = (double)decimalValue;
                return true;
            default:
                return double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out number);
        }
    }

    private static bool ContainsValue(object? haystack, object? needle)
    {
        if (haystack is null)
        {
            return false;
        }

        if (haystack is string text)
        {
            return text.Contains(needle?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        if (haystack is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (AreEquivalentValues(entry.Key?.ToString(), needle) || AreEquivalentValues(entry.Value, needle))
                {
                    return true;
                }
            }

            return false;
        }

        if (haystack is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (AreEquivalentValues(item, needle))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private abstract record ExpressionNode
    {
        public abstract object Evaluate(IDictionary<string, object> variables);
        public virtual IEnumerable<string> GetReferencedTokens() => Array.Empty<string>();
    }

    private sealed record LiteralExpressionNode(object? Value) : ExpressionNode
    {
        public override object Evaluate(IDictionary<string, object> variables) => Value ?? string.Empty;
    }

    private sealed record IdentifierExpressionNode(string Name) : ExpressionNode
    {
        public override object Evaluate(IDictionary<string, object> variables) => ResolveIdentifier(Name, variables);
        public override IEnumerable<string> GetReferencedTokens() => new[] { Name };
    }

    private sealed record FunctionExpressionNode(string Name, IReadOnlyList<ExpressionNode> Arguments) : ExpressionNode
    {
        public override object Evaluate(IDictionary<string, object> variables)
        {
            var evaluated = Arguments.Select(argument => argument.Evaluate(variables)).ToArray();

            return Name.ToLowerInvariant() switch
            {
                "eq" => RequireArity(2, evaluated, Name, args => AreEquivalentValues(args[0], args[1])),
                "ne" => RequireArity(2, evaluated, Name, args => !AreEquivalentValues(args[0], args[1])),
                "and" => RequireMinimumArity(2, evaluated, Name, args => args.All(IsTruthy)),
                "or" => RequireMinimumArity(2, evaluated, Name, args => args.Any(IsTruthy)),
                "not" => RequireArity(1, evaluated, Name, args => !IsTruthy(args[0])),
                "contains" => RequireArity(2, evaluated, Name, args => ContainsValue(args[0], args[1])),
                "startswith" => RequireArity(2, evaluated, Name, args => (args[0]?.ToString() ?? string.Empty).StartsWith(args[1]?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase)),
                "endswith" => RequireArity(2, evaluated, Name, args => (args[0]?.ToString() ?? string.Empty).EndsWith(args[1]?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase)),
                "in" => RequireMinimumArity(2, evaluated, Name, EvaluateIn),
                "format" => RequireMinimumArity(1, evaluated, Name, args => string.Format(CultureInfo.InvariantCulture, args[0]?.ToString() ?? string.Empty, args.Skip(1).ToArray())),
                _ => throw new ExpressionResolutionException($"Unsupported expression function '{Name}'.")
            };
        }

        public override IEnumerable<string> GetReferencedTokens()
            => Arguments.SelectMany(static argument => argument.GetReferencedTokens());

        private static object EvaluateIn(object?[] args)
        {
            var value = args[0];
            if (args.Length == 2 && args[1] is IEnumerable enumerable && args[1] is not string)
            {
                foreach (var item in enumerable)
                {
                    if (AreEquivalentValues(value, item))
                    {
                        return true;
                    }
                }

                return false;
            }

            for (var i = 1; i < args.Length; i++)
            {
                if (AreEquivalentValues(value, args[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static object RequireArity(int arity, object?[] args, string functionName, Func<object?[], object> callback)
        {
            if (args.Length != arity)
            {
                throw new ExpressionResolutionException($"Function '{functionName}' expects {arity} argument(s).");
            }

            return callback(args);
        }

        private static object RequireMinimumArity(int minimumArity, object?[] args, string functionName, Func<object?[], object> callback)
        {
            if (args.Length < minimumArity)
            {
                throw new ExpressionResolutionException($"Function '{functionName}' expects at least {minimumArity} argument(s).");
            }

            return callback(args);
        }
    }

    private sealed class ExpressionParser
    {
        private readonly string _text;
        private int _index;

        public ExpressionParser(string text)
        {
            _text = text;
        }

        public ExpressionNode Parse()
        {
            SkipWhitespace();
            var node = ParseExpression();
            SkipWhitespace();
            if (!IsAtEnd)
            {
                throw new ExpressionResolutionException($"Unexpected token at position {_index + 1} in expression '{_text}'.");
            }

            return node;
        }

        private ExpressionNode ParseExpression()
        {
            SkipWhitespace();
            if (IsAtEnd)
            {
                throw new ExpressionResolutionException("Expression is empty.");
            }

            if (Peek() is '\'' or '"')
            {
                return new LiteralExpressionNode(ParseQuotedString());
            }

            var token = ParseToken();
            SkipWhitespace();

            if (!IsAtEnd && Peek() == '(')
            {
                _index++;
                var arguments = new List<ExpressionNode>();
                SkipWhitespace();
                if (!IsAtEnd && Peek() == ')')
                {
                    _index++;
                    return new FunctionExpressionNode(token, arguments);
                }

                while (true)
                {
                    arguments.Add(ParseExpression());
                    SkipWhitespace();

                    if (!IsAtEnd && Peek() == ',')
                    {
                        _index++;
                        SkipWhitespace();
                        continue;
                    }

                    if (!IsAtEnd && Peek() == ')')
                    {
                        _index++;
                        break;
                    }

                    throw new ExpressionResolutionException($"Expected ',' or ')' in expression '{_text}'.");
                }

                return new FunctionExpressionNode(token, arguments);
            }

            if (bool.TryParse(token, out var boolean))
            {
                return new LiteralExpressionNode(boolean);
            }

            if (string.Equals(token, "null", StringComparison.OrdinalIgnoreCase))
            {
                return new LiteralExpressionNode(null);
            }

            if (TryConvertNumericLiteral(token, out var numericValue))
            {
                return new LiteralExpressionNode(numericValue);
            }

            return new IdentifierExpressionNode(token);
        }

        private string ParseQuotedString()
        {
            var quote = Peek();
            _index++;
            var start = _index;

            while (!IsAtEnd && Peek() != quote)
            {
                _index++;
            }

            if (IsAtEnd)
            {
                throw new ExpressionResolutionException($"Unterminated string literal in expression '{_text}'.");
            }

            var value = _text[start.._index];
            _index++;
            return value;
        }

        private string ParseToken()
        {
            var start = _index;
            while (!IsAtEnd)
            {
                var current = Peek();
                if (char.IsWhiteSpace(current) || current is '(' or ')' or ',')
                {
                    break;
                }

                _index++;
            }

            if (start == _index)
            {
                throw new ExpressionResolutionException($"Expected token at position {_index + 1} in expression '{_text}'.");
            }

            return _text[start.._index];
        }

        private static bool TryConvertNumericLiteral(string token, out object value)
        {
            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            {
                value = intValue;
                return true;
            }

            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
            {
                value = doubleValue;
                return true;
            }

            value = string.Empty;
            return false;
        }

        private void SkipWhitespace()
        {
            while (!IsAtEnd && char.IsWhiteSpace(Peek()))
            {
                _index++;
            }
        }

        private char Peek() => _text[_index];
        private bool IsAtEnd => _index >= _text.Length;
    }

    private readonly record struct ExpressionMatch(int Index, int Length, string Expression);
}

