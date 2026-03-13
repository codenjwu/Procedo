# Testing

Procedo uses three test layers.

## Unit tests

Location: `tests/Procedo.UnitTests`

Covers isolated components such as:

- YAML parsing
- execution graph building
- scheduler behavior and reliability edges
- plugin registry behavior
- expression resolver
- validation rules/options
- persistence store behavior
- observability event model and sinks
- runtime config precedence parsing
- workflow result contract shape

Run:

```powershell
dotnet test tests/Procedo.UnitTests/Procedo.UnitTests.csproj -m:1
```

## Integration tests

Location: `tests/Procedo.IntegrationTests`

Covers end-to-end engine behavior, including:

- workflow ordering and dependency execution
- failure and resume paths
- persistence-backed continuation
- restart/recovery simulation with resume
- expression + validation integration
- observability emission through runtime paths
- mini-soak stability runs

Run:

```powershell
dotnet test tests/Procedo.IntegrationTests/Procedo.IntegrationTests.csproj -m:1
```

## Contract tests

Location: `tests/Procedo.ContractTests`

Covers compatibility guarantees for structured event contracts across targets:

- `net6.0`
- `net8.0`
- `net10.0`

Run:

```powershell
dotnet test tests/Procedo.ContractTests/Procedo.ContractTests.csproj -m:1
```

## Notes

- Use `-m:1` in this environment to avoid occasional file lock contention in `obj` outputs.
- Keep schema and backward compatibility tests updated when changing observability event payloads.
